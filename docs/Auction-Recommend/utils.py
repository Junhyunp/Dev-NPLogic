# -*- coding: utf-8 -*-
from __future__ import annotations

"""
utils.py

규칙 기반 추천에서 공통적으로 쓰이는 유틸 모음.
- 안전한 날짜/숫자 파싱과 거리 계산
- 엑셀 로딩 시 파생 컬럼 보강(auction_days, 단가/총감정가)
- 주소에서 단지/건물명 추정(동/층/호 이전 문자열 활용)
- 시간창/반경/동일단지 등의 규칙 필터와 정렬(최근 낙찰 우선)
- 카테고리 판별 및 YAML 규칙 로더 헬퍼
"""

import math
import os
import re
from datetime import datetime
from typing import Any, Dict, List, Optional, Tuple, Iterable

import numpy as np
import pandas as pd
import requests
import os


# -----------------------
# 공통 유틸
# -----------------------
EPOCH = datetime(1970, 1, 1)


def parse_date(s: Any) -> Optional[datetime]:
    """넓은 입력을 datetime으로 파싱.

    - 숫자/문자열/Timestamp 허용
    - 실패 시 None 반환
    """
    try:
        if isinstance(s, datetime):
            return s
        dt = pd.to_datetime(s, errors="coerce")
        if pd.isna(dt):
            return None
        return dt.to_pydatetime() if hasattr(dt, "to_pydatetime") else dt
    except Exception:
        return None


def to_days_from_epoch(s: Any) -> Optional[int]:
    """1970-01-01 기준 경과 일수."""
    d = parse_date(s)
    if not d:
        return None
    return (d - EPOCH).days


def safe_float(x: Any) -> Optional[float]:
    """쉼표 포함 숫자 문자열도 안전하게 float 변환."""
    if x is None:
        return None
    try:
        return float(x)
    except Exception:
        try:
            return float(str(x).replace(",", ""))
        except Exception:
            return None


def haversine_distance_m(lat1, lon1, lat2, lon2) -> Optional[float]:
    """위경도 간 하버사인 거리(미터)."""
    try:
        if None in (lat1, lon1, lat2, lon2):
            return None
        R = 6371000.0
        p1, p2 = math.radians(float(lat1)), math.radians(float(lat2))
        dphi = math.radians(float(lat2) - float(lat1))
        dlmb = math.radians(float(lon2) - float(lon1))
        a = math.sin(dphi/2)**2 + math.cos(p1)*math.cos(p2)*math.sin(dlmb/2)**2
        c = 2 * math.atan2(math.sqrt(a), math.sqrt(1-a))
        return R * c
    except Exception:
        return None


# -----------------------
# 파생값/로딩/인덱스
# -----------------------
def derive_fields(row: Dict[str, Any]) -> Dict[str, Optional[float]]:
    """단가/총감정가/면적 파생값 계산.

    - 건물/토지 단가: 평 단가 기준
      building_unit_price = building_appraisal_price / (building_area / 3.305785)
      land_unit_price     = land_appraisal_price / (land_area / 3.305785)
    - 총감정가: 건물+토지 합
    """
    PYEONG = 3.305785
    b_area_m2 = safe_float(row.get("building_area"))
    l_area_m2 = safe_float(row.get("land_area"))
    b_app = safe_float(row.get("building_appraisal_price"))
    l_app = safe_float(row.get("land_appraisal_price"))

    b_area_py = (float(b_area_m2) / PYEONG) if (b_area_m2 not in (None, 0)) else None
    l_area_py = (float(l_area_m2) / PYEONG) if (l_area_m2 not in (None, 0)) else None

    if (b_app not in (None, 0)) and (l_app not in (None, 0)):
        total_app = b_app + l_app
    else:
        total_app = None
    b_unit = (float(b_app) / float(b_area_py)) if (b_app not in (None, 0) and b_area_py not in (None, 0)) else None
    l_unit = (float(l_app) / float(l_area_py)) if (l_app not in (None, 0) and l_area_py not in (None, 0)) else None

    return {
        "building_unit_price": b_unit,
        "land_unit_price": l_unit,
        "total_appraisal_price": total_app,
        "building_area": b_area_m2,
        "land_area": l_area_m2,
    }


def ensure_derived_columns(df: pd.DataFrame) -> pd.DataFrame:
    """DataFrame에 파생 컬럼이 없으면 일괄 추가."""
    need = ["building_unit_price", "land_unit_price", "total_appraisal_price"]
    missing = [c for c in need if c not in df.columns]
    if not missing:
        return df
    derived = []
    for _, row in df.iterrows():
        derived.append(derive_fields(dict(row)))
    ddf = pd.DataFrame(derived)
    for c in need:
        if c not in df.columns:
            df[c] = ddf[c]
    return df


def load_excel(path: str, sheet_name: Any = 0) -> pd.DataFrame:
    """엑셀 로드 + auction_days/단가 보강."""
    df = pd.read_excel(path, sheet_name=sheet_name)
    # 면적(m^2) 보조 컬럼 제공
    if "building_area_m" not in df.columns and "building_area" in df.columns:
        try:
            df["building_area_m"] = pd.to_numeric(df["building_area"], errors="coerce")
        except Exception:
            df["building_area_m"] = None
    if "land_area_m" not in df.columns and "land_area" in df.columns:
        try:
            df["land_area_m"] = pd.to_numeric(df["land_area"], errors="coerce")
        except Exception:
            df["land_area_m"] = None
    # auction_days 보강
    if "auction_days" not in df.columns and "auction_date" in df.columns:
        df = df.copy()
        df["auction_days"] = df["auction_date"].apply(to_days_from_epoch)
    # 파생값 보강
    df = ensure_derived_columns(df)
    return df


class InMemoryIndex:
    """간단한 인메모리 래퍼(DataFrame 보관)."""
    def __init__(self, df: pd.DataFrame):
        self.df = df.reset_index(drop=True)


def build_index(df: pd.DataFrame) -> InMemoryIndex:
    return InMemoryIndex(df)


# -----------------------
# 주소/건물명 추출(간단)
# -----------------------
def extract_apartment_name_from_address(addr: str) -> Optional[str]:
    try:
        s = str(addr or "").strip()
    except Exception:
        return None
    if not s:
        return None
    suffix = r"(?:아파트|오피스텔|푸르지오|자이|래미안|아이파크|힐스테이트|롯데캐슬|e편한세상|더샵|센트레빌|위브|푸르지오시티|파라곤|그린타운\d*차|그린타운)"
    p1 = re.compile(rf"([가-힣A-Za-z0-9·\-]+?{suffix})(?=\s*(?:제?[A-Za-z0-9\-]+동|제?\d+층|제?\d+호|$))")
    p2 = re.compile(r"(?:^|\s)([가-힣A-Za-z·\-][가-힣A-Za-z0-9·\-]{1,})(?=\s*제?[A-Za-z0-9\-]+동)")
    p3 = re.compile(r"(?:^|\s)([가-힣A-Za-z·\-][가-힣A-Za-z0-9·\-]{1,})(?=\s*제?\d+층\s*제?\d+호)")
    p4 = re.compile(r"([가-힣A-Za-z0-9·\-\s]+?)(?=\s*\d{2,4}호)")
    for pat in (p1, p2, p3, p4):
        last = None
        for m in pat.finditer(s):
            last = m
        if last:
            name = last.group(1).strip()
            name = re.sub(r"\s+", " ", name)
            if len(re.sub(r"[^가-힣A-Za-z]", "", name)) >= 2 and not re.search(r"(시|군|구|동|읍|면)$", name):
                return name
    return None


def parse_address_basic(addr: Any) -> Dict[str, Optional[str]]:
    default = {"apartment_name": None}
    try:
        s = str(addr or "").strip()
    except Exception:
        return default
    if not s:
        return default
    apt = extract_apartment_name_from_address(s)
    return {"apartment_name": apt}


# -----------------------
# 규칙 필터 도우미
# -----------------------
def within_pct(center: Any, value: Any, pct: float) -> bool:
    try:
        c = float(center)
        v = float(value)
    except Exception:
        return False
    lo, hi = c * (1 - pct), c * (1 + pct)
    return (v >= lo) and (v <= hi)


def filter_time_window(df: pd.DataFrame, days: int) -> pd.DataFrame:
    if not days:
        return df
    today = to_days_from_epoch(datetime.today())
    if today is None:
        return df
    lo = today - int(days)
    hi = today
    if "auction_days" not in df.columns:
        return df
    return df.loc[(df["auction_days"] >= lo) & (df["auction_days"] <= hi)]


def filter_radius(df: pd.DataFrame, subj: Dict[str, Any], radius_m: int) -> pd.DataFrame:
    if not radius_m:
        return df
    if ("lat" not in df.columns) or ("lon" not in df.columns):
        return df
    lat, lon = subj.get("lat"), subj.get("lon")
    if lat is None or lon is None:
        return df
    keep = []
    for _, r in df.iterrows():
        d = haversine_distance_m(lat, lon, r.get("lat"), r.get("lon"))
        if d is not None and d <= float(radius_m):
            keep.append(True)
        else:
            keep.append(False)
    return df.loc[keep]


def filter_same_apartment(df: pd.DataFrame, subj: Dict[str, Any]) -> pd.DataFrame:
    sname = (subj.get("apartment_name") or "").strip()
    if not sname or ("apartment_name" not in df.columns):
        return df.iloc[0:0]
    return df.loc[df["apartment_name"].astype(str).str.strip() == sname]


def filter_same_building(df: pd.DataFrame, subj: Dict[str, Any]) -> pd.DataFrame:
    # 간단히 동일 아파트와 동일하게 처리
    return filter_same_apartment(df, subj)


def sort_by_recency_then_distance(df: pd.DataFrame, subj: Dict[str, Any]) -> pd.DataFrame:
    if df.empty:
        return df
    df2 = df.copy()
    # 거리 계산(옵션)
    if "lat" in df2.columns and "lon" in df2.columns:
        dists: List[float] = []
        for la, lo in zip(df2["lat"], df2["lon"]):
            d = haversine_distance_m(subj.get("lat"), subj.get("lon"), la, lo) or 9_99_99_99
            dists.append(float(d))
        df2["__dist"] = np.array(dists, dtype=float)
        if "auction_days" in df2.columns:
            return df2.sort_values(["auction_days", "__dist"], ascending=[False, True]).drop(columns=["__dist"], errors="ignore")
        return df2.sort_values(["__dist"], ascending=[True]).drop(columns=["__dist"], errors="ignore")
    if "auction_days" in df2.columns:
        return df2.sort_values(["auction_days"], ascending=[False])
    return df2


# -----------------------
# 카테고리/규칙 로딩
# -----------------------
CAT_APT_OFFICETEL = "APT_OFFICETEL"
CAT_ROWHOUSE_MULTI = "ROWHOUSE_MULTI"
CAT_RETAIL_OFFICE_APT_FACTORY = "RETAIL_OFFICE_APT_FACTORY"
CAT_PLANT_WAREHOUSE_ETC = "PLANT_WAREHOUSE_ETC"
CAT_OTHER_BIG = "OTHER_BIG"


def category_from_usage(usage_raw: Any) -> str:
    u = str(usage_raw or "")
    if ("아파트" in u) or ("오피스텔" in u):
        return CAT_APT_OFFICETEL
    if ("연립" in u) or ("다세대" in u):
        return CAT_ROWHOUSE_MULTI
    if any(k in u for k in ["근린상가", "상가", "사무실", "아파트형공장"]):
        return CAT_RETAIL_OFFICE_APT_FACTORY
    if any(k in u for k in ["공장", "창고", "농가관련시설", "주요소", "위험물", "분뇨", "쓰레기", "자동차관련시설"]):
        return CAT_PLANT_WAREHOUSE_ETC
    return CAT_OTHER_BIG


def load_rule(cfg: Dict[str, Any], category_key: str, rule_index: int, similar_land: bool = False) -> Dict[str, Any]:
    rules = cfg.get("rules", {})
    cat = rules.get(category_key)
    if cat is None:
        raise KeyError(f"rules에 {category_key}가 없습니다.")
    # PLANT/OTHER는 default/land_like 구조
    if isinstance(cat, dict):
        seq = cat.get("land_like") if similar_land else cat.get("default")
    else:
        seq = cat
    if not seq:
        raise KeyError(f"{category_key} 규칙 시퀀스가 비어있습니다.")
    idx = max(1, int(rule_index)) - 1
    if idx >= len(seq):
        idx = len(seq) - 1
    rule = dict(seq[idx])
    rule["__name"] = rule.get("name") or f"rule-{rule_index}"
    return rule


# -----------------------
# 규칙 적용 엔진(DF 수준)
# -----------------------
def enrich_subject(subject_row: Dict[str, Any]) -> Dict[str, Any]:
    out = dict(subject_row)
    out.update(parse_address_basic(out.get("address")))
    out.update(derive_fields(out))
    if "auction_days" not in out or out.get("auction_days") in (None, np.nan):
        out["auction_days"] = to_days_from_epoch(out.get("auction_date"))
    return out


def apply_rule(subject_row: Dict[str, Any], idx: InMemoryIndex, rule: Dict[str, Any]) -> pd.DataFrame:
    subj = enrich_subject(subject_row)
    df = idx.df.copy()
    df = ensure_derived_columns(df)

    # 동일 시설만(3번 카테고리 등)
    if bool(rule.get("same_facility", False)):
        df = df.loc[df["usage"].astype(str) == str(subj.get("usage") or "")]

    # 시간창
    tw = int(rule.get("time_window_days", 0) or 0)
    if tw:
        df = filter_time_window(df, tw)

    # 동일 단지/건물
    if rule.get("require_same_apartment"):
        if "apartment_name" not in df.columns:
            df["apartment_name"] = df["address"].apply(extract_apartment_name_from_address)
        df = filter_same_apartment(df, subj)
    if rule.get("require_same_building"):
        if "apartment_name" not in df.columns:
            df["apartment_name"] = df["address"].apply(extract_apartment_name_from_address)
        df = filter_same_building(df, subj)

    # 반경
    rad = int(rule.get("radius_m", 0) or 0)
    if rad:
        df = filter_radius(df, subj, rad)

    # 값 범위 필터
    filters = rule.get("filters") or {}
    if filters:
        svals = derive_fields(subj)
        for k, pct in filters.items():
            if k == "building_area_pct":
                col, center = "building_area", svals.get("building_area")
            elif k == "building_unit_price_pct":
                col, center = "building_unit_price", svals.get("building_unit_price")
            elif k == "total_appraisal_price_pct":
                # 총감정가 필터링:
                #  - 주제(center): 데이터디스크에서 온 total_appraisal_price
                #  - 후보: auction_constructed.xlsx 의 appraisal_price
                col, center = "appraisal_price", svals.get("total_appraisal_price")
            elif k == "land_area_pct":
                col, center = "land_area", svals.get("land_area")
            else:
                continue
            if center in (None, 0) or col not in df.columns:
                return df.iloc[0:0]
            df = df.loc[df[col].apply(lambda v: within_pct(center, v, float(pct)))]

    # 정렬
    df = sort_by_recency_then_distance(df, subj)
    return df


def parse_rule_map(s: str) -> Dict[str, int]:
    """문자열로 전달된 카테고리별 규칙 인덱스 맵을 파싱.

    허용 표기:
      - "CAT=1,CAT2=2" 또는 "CAT:1,CAT2:2" (쉼표/세미콜론 혼용 가능)
      - "1,2,3,2,1" 처럼 5개 숫자만 주는 축약형
    카테고리 키는 다음 중 하나여야 함:
      APT_OFFICETEL, ROWHOUSE_MULTI, RETAIL_OFFICE_APT_FACTORY,
      PLANT_WAREHOUSE_ETC, OTHER_BIG
    """
    if not s:
        return {}
    s_strip = str(s).strip()
    # 1) 축약형: 5개 숫자
    m = re.match(r"^\s*(\d+)\s*[,\s]\s*(\d+)\s*[,\s]\s*(\d+)\s*[,\s]\s*(\d+)\s*[,\s]\s*(\d+)\s*$", s_strip)
    if m:
        nums = [int(m.group(i)) for i in range(1, 6)]
        order = [
            CAT_APT_OFFICETEL,
            CAT_ROWHOUSE_MULTI,
            CAT_RETAIL_OFFICE_APT_FACTORY,
            CAT_PLANT_WAREHOUSE_ETC,
            CAT_OTHER_BIG,
        ]
        return {k: v for k, v in zip(order, nums)}
    parts = re.split(r"[;,]", str(s))
    out: Dict[str, int] = {}
    for p in parts:
        p = p.strip()
        if not p:
            continue
        if "=" in p:
            k, v = p.split("=", 1)
        elif ":" in p:
            k, v = p.split(":", 1)
        else:
            continue
        k = k.strip()
        try:
            out[k] = int(v.strip())
        except Exception:
            continue
    return out


# -----------------------
# 은행 엑셀 → 주제행 추출(test.py와 동일 입력 스키마)
# -----------------------
def extract_subjects_from_bank_excel(excel_path: str, sheet_name: str = "Sheet C-1") -> pd.DataFrame:
    """은행 엑셀에서 주소/용도/면적/감정가를 추출해 표준 스키마로 반환.

    반환 컬럼(최소):
      - address, usage, area_building, area_land,
        building_appraisal_price, land_appraisal_price,
        total_appraisal_price, total_appraisal_price_by_area
    """
    df = pd.read_excel(excel_path, sheet_name=sheet_name)

    def _norm(s: str) -> str:
        return re.sub(r"[^0-9a-zA-Z가-힣]", "", str(s)).lower()

    col_map = { _norm(c): c for c in df.columns }

    def find_col(patterns) -> pd.Series:
        if isinstance(patterns, str):
            patterns = [patterns]
        for pat in patterns:
            for norm_name, orig in col_map.items():
                if re.search(pat, norm_name):
                    return df[orig]
        return pd.Series([None] * len(df))

    addr1 = find_col([r"담보소재지?1", r"소재지1"]) 
    addr2 = find_col([r"담보소재지?2", r"소재지2"]) 
    addr3 = find_col([r"담보소재지?3", r"소재지3"]) 
    addr4 = find_col([r"담보소재지?4", r"소재지4"]) 
    addr = (
        addr1.fillna("").astype(str).str.strip() + " " +
        addr2.fillna("").astype(str).str.strip() + " " +
        addr3.fillna("").astype(str).str.strip() + " " +
        addr4.fillna("").astype(str).str.strip()
    ).str.replace(r"\s+", " ", regex=True).str.strip()

    usage_raw = find_col([r"propertytype", r"용도"]).fillna("").astype(str)
    def _map_usage_bank(v: Any) -> str:
        try:
            s = str(v).strip()
        except Exception:
            return ""
        # 표준화 매핑
        if s in ("전", "답"):
            return "농지"
        if s == "단독주택":
            return "주택"
        if s == "다세대":
            return "다세대(빌라)"
        if s == "주상복합(주거)":
            return "아파트"
        if (s == "오피스텔(주거)") or ("오피스텔" in s and "주거" in s):
            return "오피스텔"
        return s
    usage_series = usage_raw.map(_map_usage_bank)
    # 식별자 컬럼 추출
    borrower_serial = find_col([r"차주\s*일련\s*번호", r"borrower.*serial", r"borrower.*no"]).astype(object)
    property_serial = find_col([r"property\s*일련\s*번호", r"property.*serial", r"property.*no", r"프로퍼티\s*일련\s*번호"]).astype(object)
    # 식별자(문자 그대로 보존)
    borrower_serial = find_col([r"차주\s*일련\s*번호", r"borrower.*serial", r"borrower.*no"]).astype(object)
    property_serial = find_col([r"property\s*일련\s*번호", r"property.*serial", r"property.*no", r"프로퍼티\s*일련\s*번호"]).astype(object)
    # 식별자: 차주 일련번호, Property 일련번호(문자 그대로 보존)
    borrower_serial = find_col([r"차주\s*일련\s*번호", r"borrower.*serial", r"borrower.*no"]).astype(object)
    property_serial = find_col([r"property\s*일련\s*번호", r"property.*serial", r"property.*no", r"프로퍼티\s*일련\s*번호"]).astype(object)
    borrower_serial = find_col([r"차주\s*일련\s*번호"]).astype(object)

    b_area_m2 = find_col([r"property.*건물면적", r"건물면적"]).apply(safe_float)
    l_area_m2 = find_col([r"property.*대지면적", r"대지면적"]).apply(safe_float)
    b_app = find_col([r"건물감정평가액.*property", r"건물감정평가액"]).apply(safe_float)
    l_app = find_col([r"토지감정평가액.*property", r"토지감정평가액"]).apply(safe_float)
    # 감정평가액합계 (Property별) 컬럼이 있으면 이를 총감정가 기준으로 사용
    tot_app_raw = find_col([r"감정평가액합계.*property", r"감정평가액합계"])
    eval_group = find_col([r"감정평가구분.*property", r"감정평가구분"]).fillna("").astype(str)
    kb_price = find_col([r"kb.*아파트.*시세", r"kb.*시세"]).apply(safe_float)

    PYEONG = 3.305785
    area_building = (b_area_m2.astype(float) / PYEONG).replace([np.inf, -np.inf], np.nan)
    area_land = (l_area_m2.astype(float) / PYEONG).replace([np.inf, -np.inf], np.nan)
    # m^2 단위도 함께 제공
    area_building_m = b_area_m2.astype(float).replace([np.inf, -np.inf], np.nan)
    area_land_m = l_area_m2.astype(float).replace([np.inf, -np.inf], np.nan)

    # 총감정가: 우선 '감정평가액합계 (Property별)' 컬럼 사용,
    # 없으면 건물/토지 감정가 합으로 계산
    total_appraisal_price = pd.to_numeric(tot_app_raw, errors="coerce")
    if total_appraisal_price.isna().all():
        b_num = pd.to_numeric(b_app, errors="coerce").fillna(0)
        l_num = pd.to_numeric(l_app, errors="coerce").fillna(0)
        total_appraisal_price = (b_num + l_num).replace(0, np.nan)

    # KB시세인 경우에는 총감정가를 KB 시세로 대체
    mask_kb = eval_group.str.contains("KB시세", na=False)
    total_appraisal_price.loc[mask_kb] = pd.to_numeric(kb_price, errors="coerce").loc[mask_kb]

    out = pd.DataFrame({
        "address": addr,
        "usage": usage_series.replace("", None),
        "borrower_serial_no": borrower_serial,
        "area_building": area_building,
        "area_land": area_land,
        "area_building_m": area_building_m,
        "area_land_m": area_land_m,
        "building_appraisal_price": b_app,
        "land_appraisal_price": l_app,
        "total_appraisal_price": total_appraisal_price,
    })

    def _price_by_area(row):
        price = safe_float(row.get("total_appraisal_price"))
        if price is None:
            return None
        u = str(row.get("usage") or "")
        if any(x in u for x in ["아파트", "오피스텔", "다세대"]):
            denom = safe_float(row.get("area_building"))
        else:
            denom = safe_float(row.get("area_land"))
        if not denom or denom == 0:
            return None
        return float(price) / float(denom)

    out["total_appraisal_price_by_area"] = out.apply(_price_by_area, axis=1)
    # 완전 빈 행 제거
    mask_nonempty = (
        out["address"].fillna("").astype(str).str.strip().ne("") |
        out[["building_appraisal_price","land_appraisal_price","total_appraisal_price"]].notna().any(axis=1)
    )
    return out.loc[mask_nonempty].reset_index(drop=True)


def prepare_subjects_output(df: pd.DataFrame, source_file: Optional[str] = None) -> pd.DataFrame:
    """test.py 저장 스키마에 맞춘 컬럼 정렬/보강.

    최종 순서:
      source_file, address, usage, area_building, area_land,
      building_appraisal_price, land_appraisal_price,
      total_appraisal_price, total_appraisal_price_by_area
    """
    ordered = [
        "source_file",
        "address",
        "usage",
        "area_building",
        "area_land",
        "building_appraisal_price",
        "land_appraisal_price",
        "total_appraisal_price",
        "total_appraisal_price_by_area",
    ]
    df2 = df.copy()
    if source_file is not None:
        df2.insert(0, "source_file", source_file)
        df2 = df2.loc[:, ~df2.columns.duplicated()]
    for c in ordered:
        if c not in df2.columns:
            df2[c] = None
    return df2[ordered]


def save_topk_to_excel(subject: dict, results: list, output_path: str) -> None:
    """추천 결과를 엑셀 파일로 저장.

    저장 컬럼: subject_case_no, case_no, usage, address, auction_date,
             winning_price, popup_url, rule, category
    """
    rows = []
    for r in results:
        comp = r.get("candidate", {})
        rows.append({
            "subject_case_no": subject.get("case_no"),
            "case_no": comp.get("case_no"),
            "usage": comp.get("usage"),
            "address": comp.get("address"),
            "auction_date": comp.get("auction_date"),
            "winning_price": comp.get("winning_price"),
            "popup_url": comp.get("popup_url"),
            "rule": (r.get("detail") or {}).get("rule"),
            "category": (r.get("detail") or {}).get("category"),
        })
    os.makedirs(os.path.dirname(os.path.abspath(output_path)), exist_ok=True)
    pd.DataFrame(rows).to_excel(output_path, index=False)


def save_results_to_csv(subject: dict, results: list, output_path: str) -> None:
    """추천 결과를 CSV로 저장(utf-8-sig).

    출력 컬럼(순서):
      rule, category, case_no, auction_date, usage, address,
      land_area, building_area, new_build_date, appraisal_price,
      building_appraisal_price, land_appraisal_price,
      valuation_base_date, winning_price, big_round,
      second_big_price, popul_url

    일부 컬럼은 보유 데이터에 없을 수 있으며, 그 경우 빈 값으로 남긴다.
    """
    ordered_cols = [
        "rule",
        "category",
        "case_no",
        "auction_date",
        "usage",
        "address",
        "land_area",
        "building_area",
        "new_build_date",
        "appraisal_price",
        "land_appraisal_price",
        "building_appraisal_price",
        "valuation_base_date",
        "winning_price",
        "big_round",
        "second_big_price",
        "popul_url",
    ]

    rows = []

    PYEONG = 3.305785

    def _to_pyeong(v: Any) -> Optional[float]:
        val = safe_float(v)
        if val in (None, 0):
            return None
        try:
            return float(val) / PYEONG
        except Exception:
            return None
    for r in results:
        comp = r.get("candidate", {}) or {}
        detail = r.get("detail") or {}

        # land_area / building_area는 평 단위로 저장
        # 우선 m^2 컬럼(land_area, building_area 또는 *_m)에서 가져와 3.305785로 나눈다.
        raw_land_m = comp.get("land_area_m")
        if raw_land_m in (None, 0):
            raw_land_m = comp.get("land_area")
        raw_build_m = comp.get("building_area_m")
        if raw_build_m in (None, 0):
            raw_build_m = comp.get("building_area")
        land_py = _to_pyeong(raw_land_m)
        build_py = _to_pyeong(raw_build_m)

        row = {
            "rule": detail.get("rule"),
            "category": detail.get("category"),
            "case_no": comp.get("case_no"),
            "auction_date": comp.get("auction_date"),
            "usage": comp.get("usage"),
            "address": comp.get("address"),
            "land_area": land_py,
            "building_area": build_py,
            "new_build_date": comp.get("new_build_date"),
            "appraisal_price": comp.get("appraisal_price"),
            "building_appraisal_price": comp.get("building_appraisal_price"),
            "land_appraisal_price": comp.get("land_appraisal_price"),
            "valuation_base_date": comp.get("valuation_base_date"),
            "winning_price": comp.get("winning_price"),
            "big_round": comp.get("big_round"),
            "second_big_price": comp.get("second_big_price"),
            "popul_url": comp.get("popup_url"),
        }
        rows.append(row)

    df = pd.DataFrame(rows)
    # 컬럼 순서 고정 및 누락 컬럼 보정
    for c in ordered_cols:
        if c not in df.columns:
            df[c] = None
    df = df[ordered_cols]

    os.makedirs(os.path.dirname(os.path.abspath(output_path)), exist_ok=True)
    df.to_csv(output_path, index=False, encoding="utf-8-sig")

# -*- coding: utf-8 -*-
from dataclasses import dataclass
from datetime import datetime
import math
import re
from typing import Any, Dict, Optional
import numpy as np
import pandas as pd

DATE_PATTERNS = [
    # 2025-01-21, 2025.01.21, 2025/01/21
    (re.compile(r"^\d{4}[-./]\d{2}[-./]\d{2}$"),),
]

def parse_date(s: Optional[str]) -> Optional[datetime]:
    """문자열/숫자/파이썬 datetime/판다스 Timestamp/NaN 모두 안전 처리."""
    if s is None:
        return None
    if isinstance(s, datetime):
        return s
    # pandas로 관대한 파싱 시도(숫자/문자/Timestamp 등 모두 처리). NaN이면 NaT → None
    try:
        dt = pd.to_datetime(s, errors="coerce")
        if getattr(pd, "isna")(dt):
            return None
        # pandas Timestamp → python datetime
        return dt.to_pydatetime() if hasattr(dt, "to_pydatetime") else dt
    except Exception:
        pass
    # 문자열 정규화 후 수동 파싱
    try:
        s_str = str(s).strip()
    except Exception:
        return None
    s_str = s_str.replace("/", "-").replace(".", "-")
    try:
        return datetime.fromisoformat(s_str)
    except Exception:
        for fmt in ("%Y-%m-%d", "%Y-%m-%d %H:%M:%S"):
            try:
                return datetime.strptime(s_str, fmt)
            except Exception:
                continue
    return None

def days_diff(d1: Optional[str], d2: Optional[str]) -> Optional[int]:
    a, b = parse_date(d1), parse_date(d2)
    if not a or not b:
        return None
    return abs((a - b).days)

def years_diff(d1: Optional[str], d2: Optional[str]) -> Optional[float]:
    dd = days_diff(d1, d2)
    return None if dd is None else dd / 365.0

def ratio_diff(a: Optional[float], b: Optional[float]) -> Optional[float]:
    if a is None or b is None or a == 0:
        return None
    return abs(b - a) / abs(a)

def bucket_score(value: Optional[float], thresholds, scores=(5,4,3,2,1,0)) -> Optional[int]:
    if value is None:
        return None
    for limit, s in zip(thresholds, scores):
        if value <= limit:
            return s
    return scores[-1]

def bool_to_score(flag: Optional[bool], true_score=5, false_score=0) -> Optional[int]:
    if flag is None:
        return None
    return true_score if bool(flag) else false_score

def haversine_distance_m(lat1, lon1, lat2, lon2) -> Optional[float]:
    if None in (lat1, lon1, lat2, lon2):
        return None
    R = 6371000.0
    p1, p2 = math.radians(lat1), math.radians(lat2)
    dphi = math.radians(lat2 - lat1)
    dlmb = math.radians(lon2 - lon1)
    a = math.sin(dphi/2)**2 + math.cos(p1)*math.cos(p2)*math.sin(dlmb/2)**2
    c = 2 * math.atan2(math.sqrt(a), math.sqrt(1-a))
    return R * c

def safe_float(x) -> Optional[float]:
    if x is None:
        return None
    try:
        return float(x)
    except Exception:
        try:
            return float(str(x).replace(",", ""))
        except Exception:
            return None

def derive_fields(row: Dict[str, Any]) -> Dict[str, Optional[float]]:
    """보유 스키마에서 파생 변수 계산.

    - 건물/토지 단가: 평 단가 기준
      building_unit_price = building_appraisal_price / (building_area / 3.305785)
      land_unit_price     = land_appraisal_price / (land_area / 3.305785)
    """
    PYEONG = 3.305785
    b_area_m2 = safe_float(row.get("building_area"))
    l_area_m2 = safe_float(row.get("land_area"))
    b_app = safe_float(row.get("building_appraisal_price"))
    l_app = safe_float(row.get("land_appraisal_price"))

    b_area_py = (float(b_area_m2) / PYEONG) if (b_area_m2 not in (None, 0)) else None
    l_area_py = (float(l_area_m2) / PYEONG) if (l_area_m2 not in (None, 0)) else None

    # 총감정가:
    #  - 우선 row["total_appraisal_price"] 값을 신뢰 (은행 엑셀의 감정평가액합계 / KB 시세 등)
    #  - 없으면 건물/토지 감정가 합으로 계산
    existing_total = safe_float(row.get("total_appraisal_price"))
    if existing_total not in (None, 0):
        total_app = existing_total
    elif (b_app is not None) and (l_app is not None):
        total_app = b_app + l_app
    else:
        total_app = None
    b_unit = (float(b_app) / float(b_area_py)) if (b_app not in (None, 0) and b_area_py not in (None, 0)) else None
    l_unit = (float(l_app) / float(l_area_py)) if (l_app not in (None, 0) and l_area_py not in (None, 0)) else None

    # Fallback: 아파트/오피스텔/다세대 등에서 건물감정가가 없고(KB시세 등) 단가가 비면
    # 총감정가/면적(평)을 활용해 평 단가를 보정한다.
    if b_unit in (None, 0):
        try:
            utext = str(row.get("usage") or "")
        except Exception:
            utext = ""
        if any(k in utext for k in ["아파트","오피스텔","다세대"]):
            tapba = safe_float(row.get("total_appraisal_price_by_area"))  # 원/평
            if tapba not in (None, 0):
                b_unit = float(tapba)  # 이미 원/평
            else:
                # 총감정가와 건물면적(평)이 있으면 계산
                tot = safe_float(row.get("total_appraisal_price"))
                ab_py = safe_float(row.get("area_building"))  # 평
                if tot not in (None, 0) and ab_py not in (None, 0):
                    b_unit = float(tot) / float(ab_py)

    return {
        "building_unit_price": b_unit,
        "land_unit_price": l_unit,
        "total_appraisal_price": total_app,
        "building_area": b_area_m2,
        "land_area": l_area_m2,
    }

# -------- 주소/용도 간단 파서(placeholder) --------
# 실제 서비스에서는 행정표준코드/지오코딩을 붙이세요.
CITY_PAT = re.compile(r"(?P<city>[^ ]+시|[^ ]+군|[^ ]+구)")
GU_PAT = re.compile(r"(?P<gu>[^ ]+구)")
DONG_PAT = re.compile(r"(?P<dong>[^ ]+동)")

def extract_apartment_name_from_address(addr: str) -> Optional[str]:
    """주소 문자열에서 단지/아파트명을 휴리스틱으로 추출한다.

    대응 예시:
      - "... 화곡푸르지오 제130동 제1층 제104호" → 화곡푸르지오
      - "... 부영그린타운3차 제14층 제1403호" → 부영그린타운3차
      - "... 호평파라곤 제티06동 제3층 제301호" → 호평파라곤
      - "... 별빛마을아파트 제1010동 제11층 제1102호" → 별빛마을아파트
    """
    try:
        s = str(addr)
    except Exception:
        return None
    s = s.strip()
    if not s:
        return None

    # 1) 대표 브랜드/접미사 기반(앞쪽 토큰 + 접미사) → 동/층/호 직전 경계 확인
    suffix = r"(?:아파트|푸르지오|자이|래미안|아이파크|힐스테이트|롯데캐슬|e편한세상|더샵|센트레빌|위브|푸르지오시티|파라곤|그린타운\d*차|그린타운)"
    p1 = re.compile(rf"([가-힣A-Za-z0-9·\-]+?{suffix})(?=\s*(?:제?[A-Za-z0-9\-]+동|제?\d+층|제?\d+호|$))")

    # 2) '제..동' 앞의 한글/영문 조합 블록(숫자 시작은 제외)
    p2 = re.compile(r"(?:^|\s)([가-힣A-Za-z·\-][가-힣A-Za-z0-9·\-]{1,})(?=\s*제?[A-Za-z0-9\-]+동)")

    # 3) '동' 없이 층/호만 있는 케이스: '... [이름] 제14층 제1403호'
    p3 = re.compile(r"(?:^|\s)([가-힣A-Za-z·\-][가-힣A-Za-z0-9·\-]{1,})(?=\s*제?\d+층\s*제?\d+호)")

    # 4) 최후: '... [이름] 104호' 패턴(너무 광범위하므로 마지막 수단)
    p4 = re.compile(r"([가-힣A-Za-z0-9·\-\s]+?)(?=\s*\d{2,4}호)")

    for pat in (p1, p2, p3, p4):
        last = None
        for m in pat.finditer(s):
            last = m
        if last:
            name = last.group(1).strip()
            name = re.sub(r"\s+", " ", name)
            # 너무 짧거나 숫자만인 경우 배제
            if len(re.sub(r"[^가-힣A-Za-z]", "", name)) < 2:
                continue
            # 동/구 같은 행정단위로 끝나는 경우는 배제
            if re.search(r"(시|군|구|동|읍|면)$", name):
                continue
            return name
    return None


def parse_address_basic(addr: Optional[str]) -> Dict[str, Optional[str]]:
    # 결측/비문자 안전 처리
    default = {"city": None, "gu": None, "dong": None, "apartment_name": None}
    if addr is None:
        return default
    try:
        import pandas as _pd
        if _pd.isna(addr):
            return default
    except Exception:
        pass
    try:
        s = str(addr).strip()
    except Exception:
        return default
    if not s or s.lower() == "nan":
        return default

    city = (CITY_PAT.search(s).group("city") if CITY_PAT.search(s) else None)
    gu = (GU_PAT.search(s).group("gu") if GU_PAT.search(s) else None)
    dong = (DONG_PAT.search(s).group("dong") if DONG_PAT.search(s) else None)

    # 보강된 아파트/단지명 추출
    apt = extract_apartment_name_from_address(s)
    return {"city": city, "gu": gu, "dong": dong, "apartment_name": apt}

def usage_group(usage_raw: Optional[str]) -> Optional[str]:
    """usage 문자열이 비정상/NaN/float여도 안전하게 그룹을 도출."""
    if usage_raw is None:
        return None
    try:
        import pandas as _pd
        if _pd.isna(usage_raw):
            return None
    except Exception:
        pass
    try:
        u = str(usage_raw)
    except Exception:
        return None
    u = u.replace("[", " ").replace("]", " ").strip()
    if "아파트" in u: return "APT"
    if "오피스텔" in u: return "OFFICETEL"
    if any(x in u for x in ["상가", "근린상가"]): return "RETAIL"
    if any(x in u for x in ["주택", "다가구", "다세대", "연립"]): return "RESIDENTIAL"
    if any(x in u for x in ["공장", "창고", "물류"]): return "INDUSTRIAL"
    if "토지" in u: return "LAND"
    if any(x in u for x in ["교육", "의료", "종교", "문화", "집회", "숙박", "목욕"]): return "COMMUNITY"
    return "OTHER"

def enrich_record_minimal(row: Dict[str, Any]) -> Dict[str, Any]:
    """주소 파싱/usage 그룹 등 최소 보강(지오코딩 없는 버전)."""
    out = dict(row)
    adr = parse_address_basic(out.get("address", ""))
    for k, v in adr.items():
        out.setdefault(k, v)
    out.setdefault("usage_group", usage_group(out.get("usage")))
    return out

def annotate_pair_flags(subj: Dict[str, Any], comp: Dict[str, Any]) -> Dict[str, Any]:
    """비교건에 플래그/거리 필드 추가."""
    out = dict(comp)
    out["_same_apartment"] = (
        bool(subj.get("apartment_name")) and bool(comp.get("apartment_name")) and
        (str(subj.get("apartment_name")).strip() == str(comp.get("apartment_name")).strip())
    )
    out["_same_dong"] = (subj.get("dong") and comp.get("dong") and subj.get("dong") == comp.get("dong"))
    out["_same_gu"] = (subj.get("gu") and comp.get("gu") and subj.get("gu") == comp.get("gu"))
    out["_same_city"] = (subj.get("city") and comp.get("city") and subj.get("city") == comp.get("city"))
    out["_same_usage_group"] = (subj.get("usage_group") and comp.get("usage_group") and subj.get("usage_group") == comp.get("usage_group"))
    out["_court_match"] = (subj.get("court") and comp.get("court") and subj.get("court") == comp.get("court"))

    lat1, lon1 = safe_float(subj.get("lat")), safe_float(subj.get("lon"))
    lat2, lon2 = safe_float(comp.get("lat")), safe_float(comp.get("lon"))
    out["_distance_m"] = haversine_distance_m(lat1, lon1, lat2, lon2)
    return out


# -----------------------
# 주소 파싱(대지역/중지역/소지역)
# -----------------------
_ADDR_RE = re.compile(
    r"(?P<province>[가-힣]+?(특별시|광역시|도))?\s*"
    r"(?P<city>[가-힣]+?(시|군|구))?\s*"
    r"(?P<town>[가-힣0-9]+?(읍|면|동|리))?"
)

def parse_korean_address(address: Optional[str]) -> Optional[Dict[str, Optional[str]]]:
    if address is None:
        return None
    try:
        s = str(address).strip()
    except Exception:
        return None
    if not s:
        return None
    m = _ADDR_RE.search(s)
    return (m.groupdict() if m else None)


# -----------------------
# VWorld 주소→좌표 (EPSG:4326)
# -----------------------
_VWORLD_URL = "https://api.vworld.kr/req/address"
# _VWORLD_KEY = os.environ.get("VWORLD_KEY", "FA8A2F9F-12A3-35E7-9397-AD6EB13529C8") # 데이터 구축용
_VWORLD_KEY = os.environ.get("VWORLD_KEY", "EB3C27E6-CCD3-3E47-953A-86BB357B364C") # 테스트용
# 키 로테이션(선택): 환경변수 VWORLD_KEYS="key1,key2,..." 로 지정 시 2만 호출마다 다음 키로 전환
_VWORLD_KEYS_ROT = [k.strip() for k in os.environ.get("VWORLD_KEYS", "").split(",") if k.strip()]
_VWORLD_CALLS_ROT = 0
_GEOCODE_CACHE: Dict[str, Tuple[Optional[float], Optional[float], Optional[str]]] = {}

def _vworld_parse_point(js: Dict[str, Any]) -> Tuple[Optional[float], Optional[float]]:
    try:
        res = js.get("response", {})
        if str(res.get("status")) != "OK":
            return None, None
        result = res.get("result")
        if isinstance(result, list) and result:
            point = result[0].get("point", {})
        else:
            point = (result or {}).get("point", {})
        x = point.get("x")
        y = point.get("y")
        return (float(x), float(y)) if x is not None and y is not None else (None, None)
    except Exception:
        return None, None

def geocode_address_vworld(address: Optional[str], prefer_type: str = "ROAD") -> Tuple[Optional[float], Optional[float], Optional[str]]:
    """주소 문자열을 VWorld API로 좌표(EPSG:4326)로 변환한다.
    - prefer_type: "ROAD" 또는 "PARCEL"
    - 반환: (lon, lat, crs) 또는 (None, None, None)
    """
    if not address:
        return None, None, None
    s = str(address).strip()
    if not s:
        return None, None, None
    if s in _GEOCODE_CACHE:
        return _GEOCODE_CACHE[s]
    types = (prefer_type, "PARCEL" if prefer_type == "ROAD" else "ROAD")
    global _VWORLD_CALLS_ROT
    for ty in types:
        try:
            # 키 선택(2만 호출마다 다음 키, 없으면 단일 키 사용)
            if _VWORLD_KEYS_ROT:
                key_idx = min(_VWORLD_CALLS_ROT // 20000, len(_VWORLD_KEYS_ROT) - 1)
                key_to_use = _VWORLD_KEYS_ROT[key_idx]
            else:
                key_to_use = _VWORLD_KEY
            params = {
                "service": "address",
                "request": "getCoord",
                "crs": "epsg:4326",
                "format": "json",
                "type": ty,
                "address": s,
                "key": key_to_use,
            }
            r = requests.get(_VWORLD_URL, params=params, timeout=6)
            _VWORLD_CALLS_ROT += 1
            if r.status_code != 200:
                continue
            x, y = _vworld_parse_point(r.json())
            if x is not None and y is not None:
                out = (x, y, "EPSG:4326")
                _GEOCODE_CACHE[s] = out
                return out
        except Exception:
            continue
    out = (None, None, None)
    _GEOCODE_CACHE[s] = out
    return out


# -----------------------
# 인덱스/로더 (엑셀 없이도 동작 가능한 최소 구현)
# -----------------------
class InMemoryIndex:
    def __init__(self, df: pd.DataFrame):
        self.df = df.reset_index(drop=True)
        # fast lookups
        self.days = self.df.get("auction_days").to_numpy(dtype=float, copy=False) if "auction_days" in self.df.columns else np.zeros(len(self.df))
        self.lats = self.df.get("lat").to_numpy(dtype=float, copy=False) if "lat" in self.df.columns else None
        self.lons = self.df.get("lon").to_numpy(dtype=float, copy=False) if "lon" in self.df.columns else None
        # by_dong map(optional)
        self.by_dong: Dict[int, np.ndarray] = {}
        if "dong_id" in self.df.columns:
            try:
                grp = self.df.groupby(self.df["dong_id"].astype(int), dropna=False).indices
                for k, idxs in grp.items():
                    self.by_dong[int(k)] = np.fromiter(idxs, dtype=int)
            except Exception:
                self.by_dong = {}


def _to_days_from_epoch(date_str: Optional[str]) -> Optional[int]:
    d = parse_date(date_str)
    if not d:
        return None
    epoch = datetime(1970, 1, 1)
    return (d - epoch).days


def load_excel(path: str, sheet_name=0) -> pd.DataFrame:
    df = pd.read_excel(path, sheet_name=sheet_name)
    # 면적(m^2) 보조 컬럼 제공
    if "building_area_m" not in df.columns and "building_area" in df.columns:
        try:
            df["building_area_m"] = pd.to_numeric(df["building_area"], errors="coerce")
        except Exception:
            df["building_area_m"] = None
    if "land_area_m" not in df.columns and "land_area" in df.columns:
        try:
            df["land_area_m"] = pd.to_numeric(df["land_area"], errors="coerce")
        except Exception:
            df["land_area_m"] = None
    # 좌표 컬럼 정규화: latitude/longitude → lat/lon
    if "lat" not in df.columns and "latitude" in df.columns:
        try:
            df["lat"] = pd.to_numeric(df["latitude"], errors="coerce")
        except Exception:
            df["lat"] = None
    if "lon" not in df.columns and "longitude" in df.columns:
        try:
            df["lon"] = pd.to_numeric(df["longitude"], errors="coerce")
        except Exception:
            df["lon"] = None
    # 파생 컬럼 보강
    for col in ("auction_date",):
        if col in df.columns:
            pass
    # 파생 단가/총액
    derived = []
    for _, row in df.iterrows():
        r = dict(row)
        derived.append(derive_fields(r))
    if derived:
        ddf = pd.DataFrame(derived)
        for c in ddf.columns:
            if c not in df.columns:
                df[c] = ddf[c]
    # auction_days
    df["auction_days"] = df.get("auction_date").apply(_to_days_from_epoch) if "auction_date" in df.columns else None
    return df


def build_index(df: pd.DataFrame) -> InMemoryIndex:
    # 필요한 파생 컬럼 보강(없으면 추가)
    if "auction_days" not in df.columns:
        df = df.copy()
        df["auction_days"] = df.get("auction_date").apply(_to_days_from_epoch)
    # 좌표 컬럼 정규화: latitude/longitude → lat/lon
    if "lat" not in df.columns and "latitude" in df.columns:
        try:
            df["lat"] = pd.to_numeric(df["latitude"], errors="coerce")
        except Exception:
            df["lat"] = None
    if "lon" not in df.columns and "longitude" in df.columns:
        try:
            df["lon"] = pd.to_numeric(df["longitude"], errors="coerce")
        except Exception:
            df["lon"] = None
    if "building_unit_price" not in df.columns or "land_unit_price" not in df.columns or "total_appraisal_price" not in df.columns:
        derived = []
        for _, row in df.iterrows():
            r = dict(row)
            derived.append(derive_fields(r))
        ddf = pd.DataFrame(derived)
        for c in ("building_unit_price", "land_unit_price", "total_appraisal_price"):
            if c not in df.columns:
                df[c] = ddf[c]
    return InMemoryIndex(df)


def time_window_candidates(idx: InMemoryIndex, center_days: int, half_window_days: int) -> np.ndarray:
    if idx.days is None or np.isnan(center_days):
        return np.arange(len(idx.df), dtype=int)
    lo = center_days - half_window_days
    hi = center_days + half_window_days
    days = np.nan_to_num(idx.days, nan=-1e15)
    mask = (days >= lo) & (days <= hi)
    return np.where(mask)[0]


def radius_candidates(idx: InMemoryIndex, lat: Optional[float], lon: Optional[float], radius_m: float) -> np.ndarray:
    if lat is None or lon is None:
        return np.empty(0, dtype=int)
    if idx.lats is None or idx.lons is None:
        # 위치 정보가 없으면 반경 후보 없음
        return np.empty(0, dtype=int)
    # 벡터화된 하버사인 계산
    lats = idx.lats
    lons = idx.lons
    valid = (~np.isnan(lats)) & (~np.isnan(lons))
    if not np.any(valid):
        return np.empty(0, dtype=int)
    la2 = np.deg2rad(lats[valid])
    lo2 = np.deg2rad(lons[valid])
    la1 = np.deg2rad(float(lat))
    lo1 = np.deg2rad(float(lon))
    dlat = la2 - la1
    dlon = lo2 - lo1
    a = np.sin(dlat / 2.0) ** 2 + np.cos(la1) * np.cos(la2) * np.sin(dlon / 2.0) ** 2
    c = 2.0 * np.arcsin(np.sqrt(a))
    d = 6371000.0 * c
    within = d <= float(radius_m)
    base_idx = np.where(valid)[0]
    return base_idx[within]


# -----------------------
# 은행 엑셀(Sheet C-1) → 추천 주제 행 추출
# -----------------------
def extract_subjects_from_bank_excel(excel_path: str, sheet_name: str = "Sheet C-1") -> pd.DataFrame:
    """은행 제공 엑셀의 "Sheet C-1"에서 추천에 필요한 최소 컬럼을 추출해 표준 포맷으로 반환.

    반환 컬럼:
      - address: 담보소재지1~4를 공백으로 결합
      - auction_date: 오늘 날짜(YYYY-MM-DD)
      - building_area: Property- 건물면적
      - building_appraisal_price: 건물 감정평가액 (Property별)
      - land_appraisal_price: 토지감정평가액 (Property별)
      - total_appraisal_price: building_appraisal_price + land_appraisal_price
      - usage: (가능하면) Property Type
    """
    df = pd.read_excel(excel_path, sheet_name=sheet_name)

    # 열 이름 정규화(한글/영문/기호 무시하고 비교)
    import re as _re
    def _norm(s: str) -> str:
        return _re.sub(r"[^0-9a-zA-Z가-힣]", "", str(s)).lower()

    col_map = { _norm(c): c for c in df.columns }

    def find_col(patterns) -> pd.Series:
        if isinstance(patterns, str):
            patterns = [patterns]
        for pat in patterns:
            # 정규식 매칭
            for norm_name, orig in col_map.items():
                if _re.search(pat, norm_name):
                    return df[orig]
        # 없으면 길이 맞는 None 시리즈 반환
        return pd.Series([None] * len(df))

    addr1 = find_col([r"담보소재지?1", r"소재지1"]) 
    addr2 = find_col([r"담보소재지?2", r"소재지2"]) 
    addr3 = find_col([r"담보소재지?3", r"소재지3"]) 
    addr4 = find_col([r"담보소재지?4", r"소재지4"]) 
    addr_parts = [addr1, addr2, addr3, addr4]
    # 담보소재지4는 맨 앞 번지만 사용(000 또는 000-000)
    def _first_lot_token(s: str) -> str:
        try:
            s2 = str(s).strip()
        except Exception:
            return ""
        if not s2:
            return ""
        m = _re.search(r"(\d{1,6}(?:-\d{1,6})?)", s2)
        if m:
            return m.group(1)
        # 숫자 패턴이 없으면 첫 토큰만 사용
        return s2.split()[0] if s2 else ""

    addr4_first = addr_parts[3].fillna("").astype(str).map(_first_lot_token)
    addr = (
        addr_parts[0].fillna("").astype(str).str.strip() + " " +
        addr_parts[1].fillna("").astype(str).str.strip() + " " +
        addr_parts[2].fillna("").astype(str).str.strip() + " " +
        addr4_first
    ).str.replace(r"\s+", " ", regex=True).str.strip()

    today = pd.Timestamp.today().strftime("%Y-%m-%d")

    def to_num(s: pd.Series) -> pd.Series:
        return s.apply(safe_float)

    # 면적(원본): 더 넓은 패턴 지원
    b_area = to_num(find_col([
        r"property.*건물면적",
        r"(건물|연면|전용)\s*면적",
    ]))
    l_area = to_num(find_col([
        r"property.*대지면적",
        r"(대지|토지)\s*면적",
    ]))
    # 감정가(원본): 더 넓은 패턴 지원
    b_app = to_num(find_col([
        r"건물감정평가액.*property", r"건물감정평가액",
        r"건물.*감정.*(가|가격|평가액)",
        r"감정가.*건물",
    ]))
    l_app = to_num(find_col([
        r"토지감정평가액.*property", r"토지감정평가액",
        r"(대지|토지).*(감정.*(가|가격|평가액))",
        r"감정가.*(대지|토지)",
    ]))
    # 기계평가액 / 제시외(부속) 평가액 등 기타 항목 (Property별)
    mach_app = to_num(find_col([
        r"기계.*평가액.*property", r"기계.*평가액",
        r"machinery.*appraisal",
    ]))
    extra_app = to_num(find_col([
        r"제시외.*평가액.*property", r"제시외.*평가액",
        r"부속.*평가액.*property", r"부속.*평가액",
        r"extra.*appraisal",
    ]))
    # 감정평가액합계 (Property별) 컬럼이 있으면 이를 총감정가 기준으로 사용
    tot_app_raw = find_col([r"감정평가액합계.*property", r"감정평가액합계"])
    # 구분: KB 시세 여부 판정(패턴 확장)
    eval_group = find_col([
        r"(감정|평가|시세).*구분.*property",
        r"(감정|평가|시세).*구분",
    ]).fillna("").astype(str)
    kb_price = to_num(find_col([r"kb.*아파트.*시세", r"kb.*시세", r"케이비.*시세", r"kb.*매매.*시세"]))
    usage_series_raw = find_col([r"propertytype", r"용도"]).fillna("").astype(str)
    def _norm_usage_bank(v: Any) -> str:
        try:
            s = str(v).strip()
        except Exception:
            return ""
        # 공백/변형 표기 정규화
        s_compact = re.sub(r"\s+", "", s)
        # 아파트형공장, 근린상가 등 합성어 통일
        if "아파트형공장" in s_compact:
            return "아파트형공장"
        if "근린상가" in s_compact or ("근린" in s and "상가" in s):
            return "근린상가"
        if s in ("전", "답"):
            return "농지"
        if s == "단독주택":
            return "주택"
        if s == "다세대":
            return "다세대(빌라)"
        if s == "주상복합(주거)":
            return "아파트"
        if (s == "오피스텔(주거)") or ("오피스텔" in s and "주거" in s):
            return "오피스텔"
        return s
    usage_series = usage_series_raw.map(_norm_usage_bank)
    # 식별자 컬럼(없으면 None)
    borrower_serial = find_col([r"차주\s*일련\s*번호", r"borrower.*serial", r"borrower.*no"]).astype(object)
    property_serial = find_col([r"property\s*일련\s*번호", r"property.*serial", r"property.*no", r"프로퍼티\s*일련\s*번호"]).astype(object)

    out = pd.DataFrame({
        "address": addr,
        "auction_date": today,
        "building_area": b_area,
        "building_appraisal_price": b_app,
        "land_appraisal_price": l_app,
        "usage": usage_series.replace("", None),
        # region_big/region_mid/region_small: 주소 문자열을 기반으로 파싱
        "borrower_serial_no": borrower_serial,
        "property_serial_no": property_serial,
    })
    parsed_region = out["address"].apply(parse_korean_address)
    out["region_big"] = parsed_region.apply(lambda d: (d or {}).get("province"))
    out["region_mid"] = parsed_region.apply(lambda d: (d or {}).get("city"))
    out["region_small"] = parsed_region.apply(lambda d: (d or {}).get("town"))

    # 총감정가: 우선 '감정평가액합계 (Property별)' 컬럼 사용,
    # 없으면 건물/토지/기계/제시외 감정가 합으로 계산
    total_appraisal_price = pd.to_numeric(tot_app_raw, errors="coerce")
    if total_appraisal_price.isna().all():
        b_num = pd.to_numeric(b_app, errors="coerce").fillna(0)
        l_num = pd.to_numeric(l_app, errors="coerce").fillna(0)
        m_num = pd.to_numeric(mach_app, errors="coerce").fillna(0)
        e_num = pd.to_numeric(extra_app, errors="coerce").fillna(0)
        total_appraisal_price = (b_num + l_num + m_num + e_num).replace(0, np.nan)
    out["total_appraisal_price"] = total_appraisal_price

    # KB시세인 경우: KB아파트시세 값을 총감정가로 사용
    mask_kb = eval_group.str.contains("KB시세", na=False) | eval_group.str.contains("KB", na=False)
    out.loc[mask_kb, "total_appraisal_price"] = pd.to_numeric(kb_price, errors="coerce").loc[mask_kb]
    # 폴백: 총감정가가 비어있고 KB시세 수치가 있으면 KB시세로 대체
    _kb_series = pd.to_numeric(kb_price, errors="coerce")
    _mask_nan_total = out["total_appraisal_price"].isna()
    _mask_kb_numeric = _kb_series.notna()
    _mask_use = _mask_nan_total & _mask_kb_numeric
    if _mask_use.any():
        out.loc[_mask_use, "total_appraisal_price"] = _kb_series.loc[_mask_use]

    # 면적(평) 파생: ㎡ → 평 (1평 = 3.305785㎡)
    PYEONG = 3.305785
    out["area_building"] = (out["building_area"].astype(float) / PYEONG).replace([np.inf, -np.inf], np.nan)
    out["area_land"] = (l_area.astype(float) / PYEONG).replace([np.inf, -np.inf], np.nan)
    # 면적(m^2)도 함께 제공
    out["area_building_m"] = out["building_area"].astype(float).replace([np.inf, -np.inf], np.nan)
    out["area_land_m"] = l_area.astype(float).replace([np.inf, -np.inf], np.nan)

    # 단가(총감정가/면적) 파생
    def per_area(row):
        import math as _math
        price = safe_float(row.get("total_appraisal_price"))
        # 가격이 None 또는 NaN이면 계산 불가
        if price is None or (isinstance(price, float) and _math.isnan(price)):
            return None
        u = str(row.get("usage") or "")
        if any(x in u for x in ["아파트", "오피스텔", "다세대"]):
            denom = safe_float(row.get("area_building"))
        else:
            denom = safe_float(row.get("area_land"))
        # 분모가 None/0/NaN이면 계산 불가
        if denom is None:
            return None
        try:
            denom_f = float(denom)
        except Exception:
            return None
        if denom_f == 0.0 or (isinstance(denom_f, float) and _math.isnan(denom_f)):
            return None
        return float(price) / denom_f

    out["total_appraisal_price_by_area"] = out.apply(per_area, axis=1)

    # 디버그: 평당감정가 또는 토지면적 NaN 원인 추적 (환경변수 DEBUG_BANK_SUBJECTS=1 일 때만 출력)
    try:
        import os as _os
        _dbg_flag = str(_os.environ.get("DEBUG_BANK_SUBJECTS", "")).strip().lower() in ("1", "true", "yes", "y")
        if _dbg_flag:
            kb_flag = eval_group.str.contains("KB시세", na=False)

            def _reason_debug(row: pd.Series) -> str:
                import math as _math
                price = safe_float(row.get("total_appraisal_price"))
                u = str(row.get("usage") or "")
                denom_name = "area_building" if any(x in u for x in ["아파트", "오피스텔", "다세대"]) else "area_land"
                denom_val = safe_float(row.get(denom_name))
                if price is None or (isinstance(price, float) and _math.isnan(price)):
                    return "price_none_or_nan"
                if denom_val is None:
                    return f"denom_none({denom_name})"
                try:
                    d = float(denom_val)
                except Exception:
                    return f"denom_invalid({denom_name})"
                if d == 0.0:
                    return f"denom_zero({denom_name})"
                if isinstance(d, float) and _math.isnan(d):
                    return f"denom_nan({denom_name})"
                return "unknown"

            _bad_mask = out["area_land"].isna() | out["total_appraisal_price_by_area"].isna()
            _bad = out.loc[_bad_mask].copy()
            if not _bad.empty:
                print(f"[debug] bank_subjects: NaN rows = {len(_bad)}")
                _bad = _bad.reset_index()
                for _, r in _bad.iterrows():
                    idx0 = int(r["index"])
                    try:
                        print(
                            "[debug] idx=", idx0,
                            "| usage=", r.get("usage"),
                            "| b_area_raw=", b_area.iloc[idx0] if idx0 < len(b_area) else None,
                            "| l_area_raw=", l_area.iloc[idx0] if idx0 < len(l_area) else None,
                            "| area_b(pyeong)=", r.get("area_building"),
                            "| area_l(pyeong)=", r.get("area_land"),
                            "| b_app=", r.get("building_appraisal_price"),
                            "| l_app=", r.get("land_appraisal_price"),
                            "| total=", r.get("total_appraisal_price"),
                            "| kb=", bool(kb_flag.iloc[idx0]) if idx0 < len(kb_flag) else None,
                            "| kb_price=", kb_price.iloc[idx0] if idx0 < len(kb_price) else None,
                            "| reason=", _reason_debug(r),
                        )
                    except Exception as _e:
                        print("[debug] print-failed idx=", idx0, "err=", _e)
    except Exception:
        pass
    # 완전 빈 행 제거(주소와 금액/면적이 모두 비어있는 경우)
    mask_nonempty = (
        out["address"].fillna("").astype(str).str.strip().ne("") |
        out[["building_area", "building_appraisal_price", "land_appraisal_price", "total_appraisal_price"]].notna().any(axis=1)
    )
    out = out.loc[mask_nonempty].reset_index(drop=True)

    # 좌표 보강(VWorld): subject 필터 정확도 향상을 위해 주제 행에도 lat/lon 생성
    def _geo_apply(addr: Any) -> pd.Series:
        try:
            s = str(addr).strip()
        except Exception:
            s = ""
        if not s:
            return pd.Series({"lon": None, "lat": None, "coord_crs": None})
        x, y, crs = geocode_address_vworld(s)
        return pd.Series({"lon": x, "lat": y, "coord_crs": crs})

    geo_df = out["address"].apply(_geo_apply)
    # 중복 컬럼 방지(이미 존재할 수도 있음)
    for col in ("lon", "lat", "coord_crs"):
        if col in out.columns:
            out.drop(columns=[col], inplace=True)
    out = pd.concat([out, geo_df], axis=1)
    return out


# -----------------------
# 테스트/저장용: 컬럼 정렬/보강
# -----------------------
def prepare_subjects_output(df: pd.DataFrame, source_file: Optional[str] = None) -> pd.DataFrame:
    """추출된 주제 DataFrame을 최종 저장 스키마로 정렬/보강한다.

    최종 컬럼 순서:
      source_file, address, usage, area_building, area_land,
      building_appraisal_price, land_appraisal_price,
      total_appraisal_price, total_appraisal_price_by_area
    """
    ordered = [
        "source_file",
        "address",
        "usage",
        "area_building",
        "area_land",
        "building_appraisal_price",
        "land_appraisal_price",
        "total_appraisal_price",
        "total_appraisal_price_by_area",
    ]
    df2 = df.copy()
    if source_file is not None:
        # 앞단에서 이미 있을 수 있으니 덮어쓰기
        df2.insert(0, "source_file", source_file)
        # 중복 컬럼 정리
        df2 = df2.loc[:, ~df2.columns.duplicated()]
    # 누락 컬럼은 채워두기
    for col in ordered:
        if col not in df2.columns:
            df2[col] = None
    return df2[ordered]

# -----------------------
# 통계: 지역별 낙찰가율(3/6/12개월) 저장
# -----------------------
def _winning_ratio(row: pd.Series) -> Optional[float]:
    win = safe_float(row.get("winning_price"))
    b = safe_float(row.get("building_appraisal_price"))
    l = safe_float(row.get("land_appraisal_price"))
    if win is None or b is None or l is None or (b + l) in (None, 0):
        return None
    return win / (b + l)


def _ensure_dir(path: str) -> None:
    import os as _os
    d = _os.path.dirname(_os.path.abspath(path))
    if d and not _os.path.exists(d):
        _os.makedirs(d, exist_ok=True)


def stat_area_for_address(address: str,
                          usage_group_key: Optional[str],
                          df_source: pd.DataFrame,
                          output_path: str,
                          windows_days=(90, 180, 365)) -> None:
    """주소 문자열에서 (대/중/소) 지역을 파싱하고, 같은 usage_group 내에서
    해당 지역 기준으로 3/6/12개월 낙찰가율 평균/건수를 저장.

    - address: 대상 주소 문자열
    - usage_group_key: 대상 usage_group (없으면 None)
    - df_source: 통합 엑셀에서 로드한 DataFrame
    - output_path: 저장 경로(results/<파일명>/stat_area.xlsx)
    - windows_days: 윈도우(일 단위)
    """
    # 대상 지역 파싱 및 기준일 계산 불가 → 윈도우는 전체기간 집계로 처리
    parsed = parse_korean_address(address) or {}
    big, mid, small = parsed.get("province"), parsed.get("city"), parsed.get("town")

    df = df_source.copy()
    if usage_group_key is not None:
        df["__usage_group"] = df.get("usage").apply(usage_group)
        df = df.loc[df["__usage_group"] == usage_group_key].copy()

    df["__days"] = df.get("auction_date").apply(_to_days_from_epoch)
    # 집계 지표: 낙찰가(원) 평균
    df["__metric"] = df.get("winning_price").apply(safe_float)

    from datetime import datetime as _dt
    today_days = _to_days_from_epoch(_dt.today().strftime("%Y-%m-%d"))

    def _metrics_for(region_col: str, region_value: Optional[str]) -> Dict[str, Optional[float]]:
        if not region_value or region_col not in df.columns:
            return {"cnt_3m": 0, "avg_3m": None, "cnt_6m": 0, "avg_6m": None, "cnt_12m": 0, "avg_12m": None}
        sub = df.loc[df[region_col] == region_value]
        out: Dict[str, Optional[float]] = {}
        for w, tag in zip(windows_days, ("3m","6m","12m")):
            mask = sub["__days"].notna() & (abs(sub["__days"] - int(today_days)) <= int(w))
            part = sub.loc[mask]
            vals = part["__metric"].dropna()
            out[f"cnt_{tag}"] = int(vals.count())
            out[f"avg_{tag}"] = float(vals.mean()) if not vals.empty else None
        return out

    def _label(v: Optional[str]) -> str:
        return v if (v and str(v).strip()) else "(미상)"

    rows = []
    rows.append({"region": _label(big), **_metrics_for("region_big", big)})
    rows.append({"region": _label(mid), **_metrics_for("region_mid", mid)})
    rows.append({"region": _label(small), **_metrics_for("region_small", small)})

    # 최종 표 컬럼(요청 포맷)
    out_df = pd.DataFrame(rows)[[
        "region",
        "cnt_3m", "avg_3m",
        "cnt_6m", "avg_6m",
        "cnt_12m", "avg_12m",
    ]]

    _ensure_dir(output_path)
    with pd.ExcelWriter(output_path, engine="openpyxl") as w:
        out_df.to_excel(w, index=False, sheet_name="stat")
