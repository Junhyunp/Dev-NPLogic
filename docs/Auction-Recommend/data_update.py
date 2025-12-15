# -*- coding: utf-8 -*-
from __future__ import annotations
r"""
auction_constructed에서
- 의심주소 (별도저장): python data_update.py --type address --excel results\auction_constructed.xlsx --login_id k.symoon --login_pw 595986 --address_scope invalid_only --updated_csv results\tmp_address_updated.csv --export_updated_only
- 의심주소 (업데이트): python data_update.py --type address --excel results\auction_constructed.xlsx --login_id k.symoon --login_pw 595986 --address_scope invalid_only --updated_csv results\tmp_address_updated.csv
- 지오코딩 실패건: python data_update.py --type address --excel results\auction_constructed.xlsx --login_id k.symoon --login_pw 595986 --address_scope geo_failed_only --from_failed_csv results\geo_update_failed.csv --updated_csv results\tmp_address_updated_geo_failed.csv --export_updated_only
- 지오코딩 보강 (파일 따로 저장): python data_update.py --type geo --excel results\auction_constructed.xlsx --output results\auction_constructed_geocoded.xlsx --max_rows 0 --sleep 0.05 --failed_csv results\update_failed.csv
- 지오코딩 보강 (덮어쓰기): python data_update.py --type geo --excel results\auction_constructed.xlsx --max_rows 0 --sleep 0.05 --failed_csv results\update_failed.csv

데이터 업데이트 유틸
- --type geo       : results/auction_constructed.xlsx 내 위경도/좌표계 결측 보강(VWorld)
- --type address   : popup_url을 열어 상세페이지에서 address/address_road 갱신(Selenium)
- --type appraisal : appraisal_price가 비어있는 행에 총감정가(total_appraisal_price)를 채워넣고, 갱신 내역을 CSV로 저장
"""
import argparse
import os
import time
from typing import Optional, Tuple, List, Dict

import pandas as pd
from tqdm import tqdm

# 지오코딩 / 파생 필드는 utils 사용
try:
    from utils import geocode_address_vworld, derive_fields, parse_korean_address
except Exception:
    geocode_address_vworld = None  # type: ignore
    derive_fields = None  # type: ignore
    parse_korean_address = None  # type: ignore


# -----------------------------
# 공통 유틸
# -----------------------------
def to_str(x: object) -> str:
    try:
        s = str(x).strip()
        return "" if s.lower() == "nan" else s
    except Exception:
        return ""


def is_missing_number(x: object) -> bool:
    if x is None:
        return True
    try:
        if pd.isna(x):
            return True
    except Exception:
        pass
    try:
        float(str(x))
        return False
    except Exception:
        return True


# -----------------------------
# GEO 모드
# -----------------------------
def pick_address(row: pd.Series) -> Tuple[Optional[str], str]:
    """주소/선호 타입 선택."""
    addr_rd = to_str(row.get("address_road"))
    addr = to_str(row.get("address"))
    if addr_rd:
        return (addr_rd, "ROAD")
    if addr:
        return (addr, "PARCEL")
    return (None, "ROAD")


def geocode_with_fallback(addr: Optional[str], prefer: str, row: pd.Series) -> Tuple[Optional[float], Optional[float], Optional[str]]:
    """VWorld 호출 + 폴백."""
    if geocode_address_vworld is None:
        return (None, None, None)
    if not addr:
        return (None, None, None)
    lon, lat, crs = geocode_address_vworld(addr, prefer_type=prefer)
    if (lon is not None) and (lat is not None):
        return lon, lat, crs
    # 폴백: 다른 타입 + 다른 주소 필드도 시도
    alt_type = "PARCEL" if prefer.upper() == "ROAD" else "ROAD"
    lon, lat, crs = geocode_address_vworld(addr, prefer_type=alt_type)
    if (lon is not None) and (lat is not None):
        return lon, lat, crs
    # 다른 주소 필드
    other = to_str(row.get("address_road")) if prefer.upper() == "PARCEL" else to_str(row.get("address"))
    if other:
        lon, lat, crs = geocode_address_vworld(other, prefer_type="ROAD")
        if (lon is None) or (lat is None):
            lon, lat, crs = geocode_address_vworld(other, prefer_type="PARCEL")
    return lon, lat, crs


def run_geo_update(df: pd.DataFrame, sleep_sec: float, max_rows: int, failed_csv: str) -> pd.DataFrame:
    # 필드 보장
    for col in ("longitude", "latitude", "coord_crs"):
        if col not in df.columns:
            df[col] = None

    mask_missing = df.apply(lambda r: is_missing_number(r.get("longitude")) or is_missing_number(r.get("latitude")), axis=1)
    idxs = list(df.index[mask_missing])
    if max_rows and max_rows > 0:
        idxs = idxs[: int(max_rows)]

    updated = 0
    failed = 0
    failed_rows: List[Dict[str, object]] = []
    updates_rows: List[Dict[str, object]] = []
    pbar = tqdm(enumerate(idxs, 1), total=len(idxs), desc="Geocoding (VWorld)", unit="row")
    for i, ridx in pbar:
        row = df.iloc[ridx]
        addr, prefer = pick_address(row)
        if not addr:
            failed += 1
            failed_rows.append({
                "row_index": int(ridx),
                "address_used": "",
                "prefer": prefer,
                "fallback_addr": "",
                "reason": "no_address",
            })
            pbar.set_postfix(updated=updated, failed=failed)
            continue
        try:
            lon, lat, crs = geocode_with_fallback(addr, prefer, row)
            if (lon is not None) and (lat is not None):
                old_lon = row.get("longitude")
                old_lat = row.get("latitude")
                df.at[ridx, "longitude"] = lon
                df.at[ridx, "latitude"] = lat
                if crs:
                    df.at[ridx, "coord_crs"] = crs
                updated += 1
                updates_rows.append({
                    "row_index": int(ridx),
                    "address_used": addr,
                    "old_longitude": old_lon,
                    "old_latitude": old_lat,
                    "new_longitude": lon,
                    "new_latitude": lat,
                    "coord_crs": df.at[ridx, "coord_crs"],
                })
            else:
                failed += 1
                failed_rows.append({
                    "row_index": int(ridx),
                    "address_used": addr,
                    "prefer": prefer,
                    "fallback_addr": (to_str(row.get("address_road")) if prefer.upper() == "PARCEL" else to_str(row.get("address"))),
                    "reason": "no_result",
                })
        except Exception as e:
            failed += 1
            failed_rows.append({
                "row_index": int(ridx),
                "address_used": addr,
                "prefer": prefer,
                "fallback_addr": (to_str(row.get("address_road")) if prefer.upper() == "PARCEL" else to_str(row.get("address"))),
                "reason": f"exception: {type(e).__name__}: {e}",
            })
        time.sleep(max(0.0, float(sleep_sec)))
        pbar.set_postfix(updated=updated, failed=failed)
        if (i % 200) == 0 or i == len(idxs):
            print(f"[geo] {i}/{len(idxs)} processed | updated={updated} failed={failed}")

    try:
        if failed_rows:
            failed_df = pd.DataFrame(failed_rows)
            os.makedirs(os.path.dirname(os.path.abspath(failed_csv)) or ".", exist_ok=True)
            failed_df.to_csv(failed_csv, index=False, encoding="utf-8-sig")
            print(f"[fail-log] {len(failed_rows)} rows → {failed_csv}")
    except Exception as e:
        print("[fail-log] 저장 실패:", e)
    return df


# -----------------------------
# APPRAISAL 모드 (popup_url에서 appraisal_price 추출/갱신)
# -----------------------------
def _parse_krw(text: str) -> Optional[int]:
    """'123,456원' 같은 문자열에서 숫자 부분만 정수로 파싱."""
    import re as _re
    try:
        if not text:
            return None
        m = _re.search(r"([\d][\d,]*)", str(text))
        if not m:
            return None
        return int(m.group(1).replace(",", ""))
    except Exception:
        return None


def _extract_appraisal_from_bidhistory(html: str) -> Optional[int]:
    """옥션 상세페이지 HTML에서 최저매각가격(appraisal_price)을 추출한다."""
    from bs4 import BeautifulSoup
    import re as _re

    try:
        soup = BeautifulSoup(html, "html.parser")
        container = soup.select_one("#bidhistory") or soup.select_one("td.tbl_history")
        if not container:
            return None
        appraisal_price: Optional[int] = None
        table = container.find("table")
        if table:
            rows = table.find_all("tr")
            header_idx = None
            for r in rows:
                ths = r.find_all("th")
                if ths:
                    header_idx = [th.get_text(" ", strip=True) for th in ths]
                    break
            if header_idx:
                try:
                    i_div = header_idx.index("구분")
                except ValueError:
                    i_div = None
                try:
                    i_price = header_idx.index("최저매각가격")
                except ValueError:
                    i_price = None
                data_started = False
                for r in rows:
                    ths = r.find_all("th")
                    if ths and [th.get_text(" ", strip=True) for th in ths] == header_idx:
                        data_started = True
                        continue
                    if not data_started:
                        continue
                    tds = r.find_all(["td", "th"])
                    if not tds:
                        continue
                    if i_div is not None and i_div < len(tds):
                        div_txt = tds[i_div].get_text(" ", strip=True).replace(" ", "")
                        if _re.fullmatch(r"(제)?1차", div_txt):
                            if i_price is not None and i_price < len(tds):
                                appraisal_price = _parse_krw(tds[i_price].get_text(" ", strip=True))
                                break
                if appraisal_price is None and data_started:
                    for r in rows:
                        tds = r.find_all("td")
                        if tds and i_price is not None and i_price < len(tds):
                            val = _parse_krw(tds[i_price].get_text(" ", strip=True))
                            if val is not None:
                                appraisal_price = val
                                break
            # 헤더 기반으로 못 찾았으면, '1차'가 포함된 행에서 금액 패턴을 직접 찾는다.
            if appraisal_price is None and table:
                rows = table.find_all("tr")
                for r in rows:
                    cells = r.find_all(["td", "th"])
                    if not cells:
                        continue
                    texts = [c.get_text(" ", strip=True).replace(" ", "") for c in cells]
                    if not any(_re.fullmatch(r"(제)?1차", t) for t in texts):
                        continue
                    for c in cells:
                        txt = c.get_text(" ", strip=True)
                        m_price = _re.search(r"([\d,]+)\s*원", txt)
                        if m_price:
                            try:
                                appraisal_price = int(m_price.group(1).replace(",", ""))
                            except Exception:
                                appraisal_price = None
                            break
                    if appraisal_price is not None:
                        break
        return appraisal_price
    except Exception:
        return None


def run_appraisal_extract(
    df: pd.DataFrame,
    login_id: str,
    login_pw: str,
    selenium_wait: int,
    sleep_sec: float,
    max_rows: int,
    failed_csv: str,
    out_csv: str,
) -> pd.DataFrame:
    """
    appraisal_price가 비어있고 popup_url이 있는 행들에 대해,
    옥션 상세페이지의 입찰내역(bidhistory)에서 최저매각가격을 추출해 appraisal_price에 채워넣는다.
    """
    # 필드 보장
    if "appraisal_price" not in df.columns:
        df["appraisal_price"] = None

    s = df.get("popup_url")
    if s is None:
        print("[appraisal] popup_url 컬럼이 없습니다.")
        return df
    mask_popup = (s.astype(str).str.strip().str.lower().ne("")) & (s.astype(str).str.lower().ne("nan"))  # type: ignore

    col = df["appraisal_price"]
    mask_missing = col.apply(is_missing_number)

    candidate_idx = set(df.index[mask_popup & mask_missing])
    idxs = sorted(candidate_idx)
    if max_rows and max_rows > 0:
        idxs = idxs[: int(max_rows)]

    if not idxs:
        print("[appraisal] 처리 대상 행이 없습니다.")
        return df

    failed_rows: List[Dict[str, object]] = []
    updated_rows: List[Dict[str, object]] = []
    updated = 0

    driver = _selenium_start_and_login(login_id, login_pw, wait_sec=int(selenium_wait))
    print("브라우저 로그인 완료")
    pbar = tqdm(enumerate(idxs, 1), total=len(idxs), desc="Fetch appraisal from popup_url", unit="row")
    for i, ridx in pbar:
        url = to_str(df.at[ridx, "popup_url"])
        if not url or not url.lower().startswith("http"):
            failed_rows.append({"row_index": int(ridx), "url": url, "reason": "no_url"})
            pbar.set_postfix(updated=updated, failed=len(failed_rows))
            continue
        try:
            old_val = df.at[ridx, "appraisal_price"]
            driver.get(url)
            html = driver.page_source or ""
            a_price = _extract_appraisal_from_bidhistory(html)
            if a_price is not None and a_price != old_val:
                df.at[ridx, "appraisal_price"] = a_price
                updated += 1
                updated_rows.append(
                    {
                        "row_index": int(ridx),
                        "case_no": to_str(df.at[ridx, "case_no"]) if "case_no" in df.columns else "",
                        "url": url,
                        "appraisal_price_old": old_val,
                        "appraisal_price_new": a_price,
                    }
                )
            else:
                failed_rows.append({"row_index": int(ridx), "url": url, "reason": "parse_fail"})
        except Exception as e:
            failed_rows.append(
                {
                    "row_index": int(ridx),
                    "url": url,
                    "reason": f"exception: {type(e).__name__}: {e}",
                }
            )
        time.sleep(max(0.0, float(sleep_sec)))
        pbar.set_postfix(updated=updated, failed=len(failed_rows))
        if (i % 200) == 0 or i == len(idxs):
            print(f"[appraisal] {i}/{len(idxs)} processed | updated={updated} failed={len(failed_rows)}")

    try:
        if failed_rows:
            failed_df = pd.DataFrame(failed_rows)
            os.makedirs(os.path.dirname(os.path.abspath(failed_csv)) or ".", exist_ok=True)
            failed_df.to_csv(failed_csv, index=False, encoding="utf-8-sig")
            print(f"[appraisal-fail-log] {len(failed_rows)} rows → {failed_csv}")
        if out_csv:
            upd_df = pd.DataFrame(updated_rows)
            os.makedirs(os.path.dirname(os.path.abspath(out_csv)) or ".", exist_ok=True)
            upd_df.to_csv(out_csv, index=False, encoding="utf-8-sig")
            print(f"[appraisal-log] {len(updated_rows)} rows → {out_csv}")
    except Exception as e:
        print("[appraisal-log] 저장 실패:", e)

    try:
        driver.quit()
    except Exception:
        pass
    return df


# -----------------------------
# ADDRESS 모드 (Selenium)
# -----------------------------
def _selenium_start_and_login(login_id: str, login_pw: str, wait_sec: int = 45):
    try:
        from selenium import webdriver
        from selenium.webdriver.chrome.service import Service
        from selenium.webdriver.common.by import By
        from selenium.webdriver.support.ui import WebDriverWait
        from selenium.webdriver.support import expected_conditions as EC
        from webdriver_manager.chrome import ChromeDriverManager
    except Exception as e:
        raise RuntimeError(f"selenium_not_available: {e}")

    service = Service(ChromeDriverManager().install())
    options = webdriver.ChromeOptions()
    options.add_argument("--start-maximized")
    driver = webdriver.Chrome(service=service, options=options)

    driver.get("https://www.auction1.co.kr/common/login_box.php")
    wait = WebDriverWait(driver, wait_sec)

    try:
        id_field = None
        for css in ["#client_id", "input[name='client_id']", "input#id", "input[name='id']", "input[type='text']"]:
            try:
                el = driver.find_element(By.CSS_SELECTOR, css)
                if el.is_displayed():
                    id_field = el
                    break
            except Exception:
                continue
        pw_field = None
        for css in ["#pw_Dummy", "input[name='pw_Dummy']", "#passwd", "input[name='passwd']", "input[type='password']", "input#password", "input[name='password']"]:
            try:
                el = driver.find_element(By.CSS_SELECTOR, css)
                if el.is_displayed():
                    pw_field = el
                    break
            except Exception:
                continue
        if id_field and pw_field:
            id_field.clear(); id_field.send_keys(login_id)
            if (pw_field.get_attribute("id") or "") == "pw_Dummy":
                pw_field.click(); import time as _t; _t.sleep(0.2)
                real_pw = None
                for sel in ["#passwd", "input[name='passwd']", "input[type='password']", "input#password", "input[name='password']"]:
                    try:
                        el2 = driver.find_element(By.CSS_SELECTOR, sel)
                        if el2.is_displayed():
                            real_pw = el2
                            break
                    except Exception:
                        continue
                (real_pw or pw_field).clear(); (real_pw or pw_field).send_keys(login_pw)
            else:
                pw_field.clear(); pw_field.send_keys(login_pw)
            # 로그인 버튼
            submitted = False
            for css in ["button[type='submit']", "input[type='submit']", "#login_btn", "button.login", "a.login", "span.btn_box_b.btn_darkblue_1", ".btn_box_b.btn_darkblue_1"]:
                try:
                    btn = driver.find_element(By.CSS_SELECTOR, css)
                    if btn.is_displayed():
                        try:
                            driver.execute_script("arguments[0].scrollIntoView({block:'center'});", btn)
                        except Exception:
                            pass
                        btn.click(); submitted = True; break
                except Exception:
                    continue
            if not submitted:
                from selenium.webdriver.common.keys import Keys
                pw_field.send_keys(Keys.ENTER)
    except Exception:
        pass

    # 로그인 완료 대기
    import time as _t
    end_t = _t.time() + wait_sec
    while _t.time() < end_t:
        cur = driver.current_url or ""
        if "login_box.php" not in cur:
            break
        _t.sleep(0.3)
    return driver


def _extract_address_pair_from_soup(html: str) -> Tuple[Optional[str], Optional[str]]:
    """옥션 상세페이지 HTML에서 (지번주소, 도로명주소) 한 쌍을 추출한다.

    규칙:
      - '소 재 지' tr → 지번(address)
      - '새 주 소' tr → 도로명(address_road)
      - 각 td 안에서 addr_view_1 개수에 따라 주소를 조립
        * 0개: td의 가시 텍스트 전체
        * 1개: td의 가시 텍스트 전체(앞 텍스트 + addr_view_1)
        * 2개 이상: 첫 번째 addr_view_1을 기본주소, 두 번째를 상세로 보고 '기본 + 상세'
    """
    from bs4 import BeautifulSoup
    import re as _re

    def _norm(s: str) -> str:
        return " ".join((s or "").split())

    def _extract_full_from_td(td) -> Optional[str]:
        if td is None:
            return None
        # 숨김 텍스트/불필요 태그 제거
        for tag in td.select("span.addr_view_0, textarea, script"):
            try:
                tag.decompose()
            except Exception:
                pass
        spans = td.select("span.addr_view_1")
        text_all = _norm(td.get_text(" ", strip=True))
        if not spans:
            return text_all or None
        if len(spans) == 1:
            # 앞 텍스트 + addr_view_1 이 이미 한 줄로 보이므로 전체를 사용
            return text_all or None
        # 2개 이상: 기본 + 상세
        base = _norm(spans[0].get_text(" ", strip=True))
        detail = _norm(spans[1].get_text(" ", strip=True))
        full = f"{base} {detail}".strip()
        return full or text_all or None

    try:
        soup = BeautifulSoup(html, "html.parser")
        parcel_td = None     # 소재지
        road_td = None       # 새 주소(도로명)
        storage_td = None    # 보관장소

        for th in soup.find_all("th"):
            try:
                txt = _norm(th.get_text(" ", strip=True))
            except Exception:
                continue
            # '소 재 지'만 인식(공백 없는 '소재지'는 제외)
            if "소 재 지" in txt:
                sib = th.find_next_sibling("td")
                if sib is not None:
                    parcel_td = sib
            # 새주소도 '새 주 소' (띄어쓰기 포함)만 인식
            elif "새 주 소" in txt:
                sib = th.find_next_sibling("td")
                if sib is not None:
                    road_td = sib
            elif ("보관장소" in txt) or ("보관 장소" in txt):
                sib = th.find_next_sibling("td")
                if sib is not None:
                    storage_td = sib

        # 1차: 정석적인 소재지/새주소에서 추출
        addr = _extract_full_from_td(parcel_td)
        addr_road = _extract_full_from_td(road_td)

        # 2차: 소재지가 없고 보관장소만 있는 경우 → 보관장소를 address로 사용
        if addr is None and storage_td is not None:
            addr = _extract_full_from_td(storage_td)

        return (addr, addr_road)
    except Exception:
        return (None, None)


def _is_suspect_address(s: str) -> bool:
    """주소가 비정상/부정확해 보이는지 휴리스틱 판정."""
    ss = to_str(s)
    if not ss:
        return True
    low = ss.lower()
    # 비주소 키워드(페이지 문구/지형/시설 등)
    bad_keywords = [
        "를 관할하는 시", "공시기준 공시가격",
        "주차장", "버스정류장", "포구", "해상", "부두",
    ]
    if any(k in ss for k in bad_keywords):
        return True
    # 정상적인 전체 주소는 보통 시/군/구가 포함된다.
    if ("시" in ss) or ("군" in ss) or ("구" in ss):
        return False
    # 시/군/구가 전혀 없고, 동/리/가 단위도 없는 경우(예: '현대아파트 108동 6층 603호')는 의심
    if not any(x in ss for x in ["동", "리", "가"]):
        return True
    # 길이가 비정상적으로 짧으면 의심
    if len(ss) <= 5:
        return True
    return True  # 시/군/구 없는 나머지 패턴은 보수적으로 의심 처리


def _is_row_suspect_address(row: pd.Series) -> bool:
    """address 또는 address_road 중 하나라도 이상하면 True."""
    a = to_str(row.get("address"))
    r = to_str(row.get("address_road"))
    return _is_suspect_address(a) or _is_suspect_address(r)


def run_address_update(
    df: pd.DataFrame,
    login_id: str,
    login_pw: str,
    selenium_wait: int,
    sleep_sec: float,
    max_rows: int,
    failed_csv: str,
    address_scope: str = "invalid_only",
    from_failed_csv: str = "",
    updated_csv: str = "",
) -> pd.DataFrame:
    # 필드 보장
    if "address" not in df.columns:
        df["address"] = None
    if "address_road" not in df.columns:
        df["address_road"] = None
    # region_big / region_mid / region_small 필드 보장
    for col in ("region_big", "region_mid", "region_small"):
        if col not in df.columns:
            df[col] = None
    # 처리 대상: popup_url 유효
    s = df.get("popup_url")
    if s is None:
        print("[warn] popup_url 컬럼이 없습니다.")
        return df
    mask_popup = (s.astype(str).str.strip().str.lower().ne("")) & (s.astype(str).str.lower().ne("nan"))  # type: ignore

    # 범위 선택
    candidate_idx = set(df.index[mask_popup])
    if address_scope == "empty_only":
        mask_empty = df["address"].astype(str).str.strip().eq("") | df["address"].isna()
        candidate_idx &= set(df.index[mask_empty])
    elif address_scope == "invalid_only":
        # address와 address_road 중 어느 하나라도 이상한 행만 포함
        mask_invalid = df.apply(_is_row_suspect_address, axis=1)
        candidate_idx &= set(df.index[mask_invalid])
    elif address_scope == "geo_failed_only":
        # 위경도 결측건만
        mask_geo = df.apply(lambda r: is_missing_number(r.get("longitude")) or is_missing_number(r.get("latitude")), axis=1)
        candidate_idx &= set(df.index[mask_geo])
    # from_failed_csv가 있으면 교집합으로 축소
    if from_failed_csv and os.path.isfile(from_failed_csv):
        try:
            _tmp = pd.read_csv(from_failed_csv)
            row_set = set(map(int, (_tmp.get("row_index") or [])))
            candidate_idx &= row_set
        except Exception as e:
            print("[warn] from_failed_csv 읽기 실패:", e)

    idxs = sorted(candidate_idx)
    if max_rows and max_rows > 0:
        idxs = idxs[: int(max_rows)]

    failed_rows: List[Dict[str, object]] = []
    updated_rows: List[Dict[str, object]] = []
    updated = 0

    driver = _selenium_start_and_login(login_id, login_pw, wait_sec=int(selenium_wait))
    print("브라우저 로그인 완료")
    pbar = tqdm(enumerate(idxs, 1), total=len(idxs), desc="Fetch address from popup_url", unit="row")
    for i, ridx in pbar:
        url = to_str(df.at[ridx, "popup_url"])
        if not url or not url.lower().startswith("http"):
            failed_rows.append({"row_index": int(ridx), "url": url, "reason": "no_url"})
            pbar.set_postfix(updated=updated, failed=len(failed_rows)); continue
        try:
            old_addr = to_str(df.at[ridx, "address"]) if "address" in df.columns else ""
            old_road = to_str(df.at[ridx, "address_road"]) if "address_road" in df.columns else ""
            driver.get(url)
            html = driver.page_source or ""
            addr_main, addr_road = _extract_address_pair_from_soup(html)
            changed = False
            new_addr = old_addr
            new_road = old_road
            # 주소(소재지/보관장소 기준)
            if addr_main and addr_main != old_addr:
                df.at[ridx, "address"] = addr_main
                new_addr = addr_main
                changed = True
            # 도로명 주소:
            #  - 상세페이지에서 새주소(도로명)를 파싱했다면 그 값으로 갱신
            #  - 새주소가 전혀 없으면 기존 address_road는 신뢰하지 않고 초기화
            if addr_road is not None and addr_road != old_road:
                df.at[ridx, "address_road"] = addr_road
                new_road = addr_road
                changed = True
            elif addr_road is None and old_road:
                df.at[ridx, "address_road"] = None
                new_road = None
                changed = True
            # 주소 기준 region_big / region_mid / region_small 재계산
            try:
                if parse_korean_address is not None:
                    base_addr = addr_main or df.at[ridx, "address"]
                    if base_addr:
                        parsed = parse_korean_address(base_addr) or {}
                        df.at[ridx, "region_big"] = parsed.get("province")
                        df.at[ridx, "region_mid"] = parsed.get("city")
                        df.at[ridx, "region_small"] = parsed.get("town")
            except Exception:
                # 지역 파싱 실패는 치명적이지 않으므로 무시
                pass
            if changed:
                updated += 1
                updated_rows.append({
                    "row_index": int(ridx),
                    "case_no": to_str(df.at[ridx, "case_no"]) if "case_no" in df.columns else "",
                    "url": url,
                    "address_old": old_addr,
                    "address_new": new_addr,
                    "address_road_old": old_road,
                    "address_road_new": new_road,
                })
            else:
                failed_rows.append({"row_index": int(ridx), "url": url, "reason": "parse_fail"})
        except Exception as e:
            failed_rows.append({"row_index": int(ridx), "url": url, "reason": f"exception: {type(e).__name__}: {e}"})
        time.sleep(max(0.0, float(sleep_sec)))
        pbar.set_postfix(updated=updated, failed=len(failed_rows))
        if (i % 200) == 0 or i == len(idxs):
            print(f"[addr] {i}/{len(idxs)} processed | updated={updated} failed={len(failed_rows)}")
    try:
        driver.quit()
    except Exception:
        pass

    try:
        if failed_rows:
            failed_df = pd.DataFrame(failed_rows)
            os.makedirs(os.path.dirname(os.path.abspath(failed_csv)) or ".", exist_ok=True)
            failed_df.to_csv(failed_csv, index=False, encoding="utf-8-sig")
            print(f"[fail-log] {len(failed_rows)} rows → {failed_csv}")
        if updated_csv:
            upd_df = pd.DataFrame(updated_rows)
            os.makedirs(os.path.dirname(os.path.abspath(updated_csv)) or ".", exist_ok=True)
            upd_df.to_csv(updated_csv, index=False, encoding="utf-8-sig")
            print(f"[updated-log] {len(updated_rows)} rows → {updated_csv}")
    except Exception as e:
        print("[fail-log] 저장 실패:", e)
    return df


# -----------------------------
# CLI
# -----------------------------
def main() -> None:
    p = argparse.ArgumentParser(description="엑셀 데이터 보강(geocoding/address/appraisal)")
    p.add_argument("--type", choices=["geo", "address", "appraisal"], required=True, help="업데이트 유형")
    p.add_argument("--excel", default="results/auction_constructed.xlsx", help="입력 엑셀 경로")
    p.add_argument("--sheet", type=int, default=0, help="시트 인덱스(기본 0)")
    p.add_argument("--output", default="", help="출력 경로(미지정 시 입력 경로에 덮어쓰기)")
    p.add_argument("--failed_csv", default="results/update_failed.csv", help="실패 행을 저장할 CSV 경로")
    p.add_argument("--sleep", type=float, default=0.05, help="요청 간 대기(초)")
    p.add_argument("--max_rows", type=int, default=0, help="최대 처리 행 수(0=전체)")
    # address 모드
    p.add_argument("--login_id", default=os.environ.get("AUCT_ID", ""), help="옥션 사이트 로그인 ID(address 모드)")
    p.add_argument("--login_pw", default=os.environ.get("AUCT_PW", ""), help="옥션 사이트 로그인 PW(address 모드)")
    p.add_argument("--selenium_wait", type=int, default=45, help="셀레니움 로그인/로딩 대기(초)")
    p.add_argument("--address_scope", choices=["all", "invalid_only", "empty_only", "geo_failed_only"], default="invalid_only", help="address 업데이트 범위")
    p.add_argument("--from_failed_csv", default="", help="해당 CSV의 row_index만 대상으로 제한(선택)")
    p.add_argument("--updated_csv", default="results/tmp_address_updated.csv", help="업데이트된 행만 별도 CSV로 저장(경로)")
    p.add_argument("--export_updated_only", action="store_true", help="엑셀은 쓰지 않고 업데이트 행만 CSV로 저장")
    # appraisal 모드
    p.add_argument("--appraisal_csv", default="results/appraisal_missing.csv", help="appraisal_price 비어있는 행의 총감정가를 저장할 CSV 경로")
    args = p.parse_args()

    if not os.path.isfile(args.excel):
        raise FileNotFoundError(args.excel)
    df = pd.read_excel(args.excel, sheet_name=int(args.sheet))

    if args.type == "geo":
        df = run_geo_update(df, sleep_sec=float(args.sleep), max_rows=int(args.max_rows or 0), failed_csv=str(args.failed_csv))
    elif args.type == "address":
        if not args.login_id or not args.login_pw:
            raise RuntimeError("address 모드에는 --login_id/--login_pw (또는 AUCT_ID/AUCT_PW 환경변수)가 필요합니다.")
        df = run_address_update(
            df,
            login_id=str(args.login_id),
            login_pw=str(args.login_pw),
            selenium_wait=int(args.selenium_wait),
            sleep_sec=float(args.sleep),
            max_rows=int(args.max_rows or 0),
            failed_csv=str(args.failed_csv),
            address_scope=str(args.address_scope),
            from_failed_csv=str(args.from_failed_csv),
            updated_csv=str(args.updated_csv),
        )
    else:  # appraisal
        if not args.login_id or not args.login_pw:
            raise RuntimeError("appraisal 모드에는 --login_id/--login_pw (또는 AUCT_ID/AUCT_PW 환경변수)가 필요합니다.")
        df = run_appraisal_extract(
            df,
            login_id=str(args.login_id),
            login_pw=str(args.login_pw),
            selenium_wait=int(args.selenium_wait),
            sleep_sec=float(args.sleep),
            max_rows=int(args.max_rows or 0),
            failed_csv=str(args.failed_csv),
            out_csv=str(args.appraisal_csv),
        )

    # 결과 저장
    # address + export_updated_only 인 경우만 엑셀 저장 생략
    if not (args.type == "address" and bool(args.export_updated_only)):
        out = args.output or args.excel
        os.makedirs(os.path.dirname(os.path.abspath(out)) or ".", exist_ok=True)
        df.to_excel(out, index=False)
        print(f"[done] rows={len(df)} → {out}")
    else:
        print(f"[done] export_updated_only: 엑셀은 저장하지 않았습니다. 업데이트 행은 {args.updated_csv}를 확인하세요.")


if __name__ == "__main__":
    main()


