# -*- coding: utf-8 -*-
from __future__ import annotations

"""
recommend.py

규칙 기반 추천의 상위 오케스트레이션.
- 설정 파일 로더(YAML/JSON)
- recommend_by_rule: 주제/인덱스/규칙을 받아 DataFrame 필터링을 위임하고 Top-K 결과를 구성
"""

import glob
import json
import os
from typing import Any, Dict, List

try:
    import yaml  # type: ignore
except Exception:
    yaml = None

import pandas as pd

from utils import (
    InMemoryIndex,
    build_index,
    load_excel,
    category_from_usage,
    load_rule,
    apply_rule,
    enrich_subject,
    parse_rule_map,
    haversine_distance_m,
)


# -----------------------
# 설정 로더 (YAML/JSON 지원)
# -----------------------
 


# -----------------------
# 규칙 기반 추천
# -----------------------
 


def run_batch_from_json(args, save_fn) -> None:
    """--json_glob 기준으로 JSON들을 순회하며 규칙 기반 추천을 실행하고 저장.

    save_fn(subject, results, out_path)를 이용해 외부 저장 형식을 주입받는다.
    """
    cfg = load_config(args.config)
    df = load_excel(args.excel, sheet_name=args.sheet)
    idx = build_index(df)

    paths = []
    for pattern in str(args.json_glob).split(";"):
        for p in glob.glob(pattern.strip()):
            paths.append(p)
    if not paths:
        raise SystemExit(f"No json files matched: {args.json_glob}")

    project_root = os.path.dirname(os.path.abspath(__file__))
    for p in paths:
        base = os.path.splitext(os.path.basename(p))[0]
        with open(p, "r", encoding="utf-8") as f:
            doc = json.load(f)
        subj = dict(doc)
        results = recommend_by_rule(
            subj,
            idx,
            cfg,
            rule_index=args.rule_index,
            similar_land=args.similar_land,
            category_override=args.category_override if args.category_override else None,
            topk=args.topk,
        )
        out_path = os.path.join(project_root, "results", base, "recommend.xlsx")
        save_fn(subj, results, out_path)
        print(f"Saved: {out_path} (top-{args.topk})")

# -*- coding: utf-8 -*-
import json
from typing import Any, Dict, List, Tuple
try:
    import yaml  # type: ignore
except Exception:
    yaml = None  # yaml이 없으면 JSON만 지원
import numpy as np
import pandas as pd

from utils import (
    derive_fields, ratio_diff, days_diff, years_diff, bucket_score, bool_to_score,
    enrich_record_minimal, annotate_pair_flags, usage_group,
    InMemoryIndex, time_window_candidates, radius_candidates
)

# -----------------------
# 설정 로더 (YAML/JSON 지원)
# -----------------------
def load_config(path: str) -> Dict[str, Any]:
    with open(path, "r", encoding="utf-8") as f:
        lower = path.lower()
        if lower.endswith((".yaml", ".yml")):
            if yaml is None:
                raise ImportError("PyYAML이 설치되어 있지 않아 YAML 구성을 읽을 수 없습니다. 'pip install PyYAML' 후 재시도하세요.")
            return yaml.safe_load(f)
        return json.load(f)

# -----------------------
# 프로파일/임계치 헬퍼
# -----------------------
def _get_feature_thresholds(cfg: Dict[str, Any], feature_key: str, profile_key: str) -> List[float]:
    """
    프로파일의 threshold_map에 지정된 키를 우선 사용.
    없으면 합리적 디폴트:
      - building_unit_price_ratio_diff -> ratio_diff_apt (있으면) -> ratio_diff
      - *_ratio_diff (그 외)           -> ratio_diff_30  (있으면) -> ratio_diff
      - distance_m / recency_days 등은 자신의 이름 그대로 사용
    """
    th = cfg["thresholds"]
    prof = cfg.get("profiles", {}).get(profile_key, {})
    thmap = prof.get("threshold_map", {})

    # 1) 명시 매핑
    mapped = thmap.get(feature_key)
    if mapped:
        return th[mapped]

    # 2) 디폴트 룰
    if feature_key == "building_unit_price_ratio_diff":
        if "ratio_diff_apt" in th:
            return th["ratio_diff_apt"]
        return th.get("ratio_diff", [0.10,0.20,0.30,0.40,0.50])

    if feature_key.endswith("ratio_diff"):
        if "ratio_diff_30" in th:
            return th["ratio_diff_30"]
        return th.get("ratio_diff", [0.10,0.20,0.30,0.40,0.50])

    # recency_days, distance_m 등은 자기 이름
    return th[feature_key]

def _active_features(cfg: Dict[str, Any], profile_key: str) -> List[str]:
    prof = cfg.get("profiles", {}).get(profile_key, {})
    return prof.get("use", [])


def _get_fallback(cfg: Dict[str, Any]) -> Dict[str, Any]:
    fb = cfg.get("fallback") or {}
    try:
        min_candidates = int(fb.get("min_candidates", 200))
    except Exception:
        min_candidates = 200
    steps = fb.get("distance_steps_m", [500, 1000, 3000, 5000, 10000])
    return {"min_candidates": min_candidates, "distance_steps_m": steps}

# -----------------------
# 단일 후보 점수화
# -----------------------
def _score_single(subject: Dict[str, Any], comp: Dict[str, Any], cfg: Dict[str, Any], profile_key: str) -> Tuple[float, Dict[str, Any]]:
    weights = cfg["weights"]
    SCORES = cfg.get("scores", [5,4,3,2,1,0])
    active = set(_active_features(cfg, profile_key))

    # 최소 보강(주소/usage) + 페어 플래그/거리
    subj = enrich_record_minimal(subject)
    base_comp = enrich_record_minimal(comp)
    comp_anno = annotate_pair_flags(subj, base_comp)

    # 파생값
    ds, dc = derive_fields(subj), derive_fields(comp_anno)

    parts: List[Tuple[str, Any]] = []

    # 기간
    if "recency_days" in active:
        recency = days_diff(subj.get("auction_date"), comp_anno.get("auction_date"))
        s = bucket_score(recency, _get_feature_thresholds(cfg, "recency_days", profile_key), SCORES)
        parts.append(("recency_days", s))

    # 위치
    if "same_apartment" in active:
        parts.append(("same_apartment", bool_to_score(comp_anno.get("_same_apartment"))))
    if "same_dong" in active:
        parts.append(("same_dong", bool_to_score(comp_anno.get("_same_dong"))))
    if "distance_m" in active:
        dist_th = _get_feature_thresholds(cfg, "distance_m", profile_key)
        parts.append(("distance_m", bucket_score(comp_anno.get("_distance_m"), dist_th, SCORES)))

    # 가격/면적 (비율차 버킷)
    if "building_unit_price_ratio_diff" in active:
        b_th = _get_feature_thresholds(cfg, "building_unit_price_ratio_diff", profile_key)
        parts.append(("building_unit_price_ratio_diff",
                      bucket_score(ratio_diff(ds["building_unit_price"], dc["building_unit_price"]), b_th, SCORES)))
    if "land_unit_price_ratio_diff" in active:
        l_th = _get_feature_thresholds(cfg, "land_unit_price_ratio_diff", profile_key)
        parts.append(("land_unit_price_ratio_diff",
                      bucket_score(ratio_diff(ds["land_unit_price"], dc["land_unit_price"]), l_th, SCORES)))
    if "total_appraisal_ratio_diff" in active:
        t_th = _get_feature_thresholds(cfg, "total_appraisal_ratio_diff", profile_key)
        # 총감정가 비교:
        #  - 주제(center): 데이터디스크에서 온 total_appraisal_price
        #  - 후보: auction_constructed.xlsx 의 appraisal_price
        subj_total = ds.get("total_appraisal_price")
        cand_total = comp_anno.get("appraisal_price")
        if (subj_total is not None) and (cand_total is not None):
            parts.append(
                (
                    "total_appraisal_ratio_diff",
                    bucket_score(ratio_diff(subj_total, cand_total), t_th, SCORES),
                )
            )
    if "land_area_ratio_diff" in active:
        a_th = _get_feature_thresholds(cfg, "land_area_ratio_diff", profile_key)
        parts.append(("land_area_ratio_diff",
                      bucket_score(ratio_diff(ds["land_area"], dc["land_area"]), a_th, SCORES)))

    # 가중합(결측은 제외-정규화)
    num = 0.0
    den = 0.0
    detail: Dict[str, Any] = {}

    for key, sc in parts:
        if sc is None:
            continue
        w = float(weights.get(key, 0.0))
        if w == 0.0:
            continue
        num += w * sc
        den += w
        detail[key] = {"score": sc, "weight": w}

    total = (num / den) if den > 0 else 0.0
    return total, detail

# -----------------------
# Top-K 추천
# -----------------------
def recommend_top_k(subject: Dict[str, Any], candidates: List[Dict[str, Any]], cfg: Dict[str, Any], k: int = 2) -> List[Dict[str, Any]]:
    profile_key = _infer_profile_for_subject(subject, cfg)
    scored = []
    for c in candidates:
        total, detail = _score_single(subject, c, cfg, profile_key)
        scored.append({
            "case_no": c.get("case_no"),
            "score": round(total, 6),
            "detail": detail,
            "candidate": c
        })

    # 동점 타이브레이커: recency → distance
    def sort_key(item):
        sub = subject
        comp = item["candidate"]
        rec = days_diff(sub.get("auction_date"), comp.get("auction_date")) or 999999
        dist = comp.get("_distance_m") or 999999
        return (-item["score"], rec, dist)

    scored.sort(key=sort_key)
    return scored[:k]

def _infer_profile_for_subject(subject: Dict[str, Any], cfg: Dict[str, Any]) -> str:
    # main.py에서 이미 선택해 넘기는 흐름이지만, 안전망으로 subject.usage_group을 참조
    ug = subject.get("usage_group") or "OTHER"
    # main.py의 PROFILE_MAP과 동일한 기본 매핑
    if ug == "APT":
        return "APT"
    if ug in ("INDUSTRIAL","LAND"):
        return "INDUSTRIAL_OR_LAND"
    if ug in ("RETAIL","RESIDENTIAL","OFFICETEL","OTHER"):
        return "RETAIL_OR_RESIDENTIAL_OR_OFFICETEL_OR_APT_FACTORY_OR_LODGING"
    return "COMMUNITY_ETC"

# -----------------------
# 후보 생성 (엑셀 인덱스 기반)
# -----------------------
def fetch_candidates_from_index(subject_row: Dict[str, Any], idx: InMemoryIndex, cfg: Dict[str, Any], profile_key: str, limit_each: int = 500) -> List[Dict[str, Any]]:
    df = idx.df
    use = set(_active_features(cfg, profile_key))
    th = cfg["thresholds"]
    fb = _get_fallback(cfg)

    # 기간창: recency_days 최댓값 사용
    tmax = th["recency_days"][-1]  # 예: 1095일(3년)
    s_days = subject_row.get("auction_days")
    if pd.notna(s_days):
        base_time_idx = time_window_candidates(idx, int(s_days), int(tmax))
    else:
        base_time_idx = np.arange(len(df), dtype=int)

    cand_idx = base_time_idx

    # 1) 같은 동(있으면 강력 필터)
    if "same_dong" in use and "dong_id" in df.columns:
        did = subject_row.get("dong_id")
        if did is not None and did != -1 and did in idx.by_dong:
            cand_idx = np.intersect1d(cand_idx, idx.by_dong[int(did)], assume_unique=False)

    # 1.5) 같은 usage_group으로 제한(요청 사항)
    subj_group = subject_row.get("usage_group") or usage_group(subject_row.get("usage"))
    if subj_group:
        if "usage_group" in df.columns:
            mask = (df["usage_group"] == subj_group)
        else:
            # on-the-fly 파생
            mask = df["usage"].apply(usage_group) == subj_group
        cand_idx = np.intersect1d(cand_idx, np.where(mask)[0], assume_unique=False)

    # 2) 거리 폴백: 최소 후보 미만이면 거리 확장 단계 적용
    need = fb["min_candidates"]
    if cand_idx.size < need and "distance_m" in use:
        lat, lon = subject_row.get("lat"), subject_row.get("lon")
        for r in fb["distance_steps_m"]:
            ring = radius_candidates(idx, lat, lon, r)
            if ring.size:
                cand_idx = np.union1d(cand_idx, np.intersect1d(base_time_idx, ring))
            if cand_idx.size >= need:
                break

    # 3) 단가/총액/면적 범위 컷(1차 후보 폭: 최상 버킷 경계 사용)
    sub_bu = subject_row.get("building_unit_price")
    sub_lu = subject_row.get("land_unit_price")
    sub_tot = subject_row.get("total_appraisal_price")
    sub_larea = subject_row.get("land_area")

    def apply_range(colname: str, center_val: float, feature_key: str):
        nonlocal cand_idx
        if center_val is None or colname not in df.columns or colname not in df:
            return
        # feature별 임계치 배열에서 최상 버킷(첫 값)을 1차 컷 폭으로 활용
        ratio_arr = _get_feature_thresholds(cfg, feature_key, profile_key)
        if not ratio_arr:
            return
        ratio = ratio_arr[0]
        lo, hi = center_val * (1 - ratio), center_val * (1 + ratio)
        mask = df[colname].between(lo, hi, inclusive="both")
        cand_idx = np.intersect1d(cand_idx, np.where(mask)[0], assume_unique=False)

    if "building_unit_price_ratio_diff" in use:
        apply_range("building_unit_price", sub_bu, "building_unit_price_ratio_diff")
    if "land_unit_price_ratio_diff" in use:
        apply_range("land_unit_price", sub_lu, "land_unit_price_ratio_diff")
    if "total_appraisal_ratio_diff" in use:
        # 후보 DF에는 appraisal_price가 존재하면 그 컬럼으로 범위 적용
        col_tot = "appraisal_price" if ("appraisal_price" in df.columns) else "total_appraisal_price"
        apply_range(col_tot, sub_tot, "total_appraisal_ratio_diff")
    if "land_area_ratio_diff" in use:
        apply_range("land_area", sub_larea, "land_area_ratio_diff")

    # 4) 상한 캡(점수화 비용 관리)
    if cand_idx.size > limit_each:
        cand_idx = cand_idx[:limit_each]

    return df.loc[cand_idx].to_dict(orient="records")


# -----------------------
# 규칙 기반 추천 (가중치 점수 대신 규칙 필터)
# -----------------------

_CAT1_APT_OFFICETEL = "APT_OFFICETEL"
_CAT2_ROWHOUSE_MULTI = "ROWHOUSE_MULTI"
_CAT3_RETAIL_OFFICE_APT_FACTORY = "RETAIL_OFFICE_APT_FACTORY"
_CAT4_PLANT_WAREHOUSE_ETC = "PLANT_WAREHOUSE_ETC"
_CAT5_OTHER_BIG = "OTHER_BIG"

def _today_days() -> int:
    from datetime import datetime as _dt
    epoch = _dt(1970, 1, 1)
    return int((_dt.today() - epoch).days)


def _within_pct(center: Any, value: Any, pct: float) -> bool:
    try:
        c = float(center)
        v = float(value)
    except Exception:
        return False
    if pct is None:
        return True
    lo, hi = c * (1 - pct), c * (1 + pct)
    return (v >= lo) and (v <= hi)


def category_from_usage(usage_raw: str, similar_land: bool = False) -> str:
    u = (usage_raw or "")
    # 1) 아파트형공장 우선 처리(아파트/공장 키워드보다 먼저)
    if "아파트형공장" in u:
        return _CAT3_RETAIL_OFFICE_APT_FACTORY
    # 2) 아파트/오피스텔
    if ("아파트" in u) or ("오피스텔" in u):
        return _CAT1_APT_OFFICETEL
    # 3) 연립/다세대
    if ("연립" in u) or ("다세대" in u):
        return _CAT2_ROWHOUSE_MULTI
    # 4) 근린상가/사무실 + (숙박/교육/종교/의료/목욕/노유자/문화집회)
    if any(k in u for k in [
        "근린상가","상가","사무실",
        "숙박시설","숙박(콘도등)","교육시설","종교시설","의료시설","목욕탕","노유자시설","문화및집회시설",
    ]):
        return _CAT3_RETAIL_OFFICE_APT_FACTORY
    # 5) 창고/공장/농가관련/주유소(위험물)/분뇨·쓰레기/자동차관련 → 산업군
    if any(k in u for k in ["공장","창고","농가관련시설","주유소(위험물)","분뇨","쓰레기","자동차관련시설"]):
        return _CAT4_PLANT_WAREHOUSE_ETC
    # 6) 그 외 전부
    return _CAT5_OTHER_BIG


def _load_rule(cfg: Dict[str, Any], category_key: str, rule_index: int, similar_land: bool) -> Dict[str, Any]:
    rules = cfg.get("rules", {})
    cat = rules.get(category_key)
    if cat is None:
        raise KeyError(f"rules에 {category_key}가 정의되지 않았습니다.")
    # PLANT/OTHER 카테고리는 default/land_like 분기
    if isinstance(cat, dict):
        seq = cat.get("land_like") if similar_land else cat.get("default")
    else:
        seq = cat
    if not seq:
        raise KeyError(f"{category_key} 규칙 시퀀스가 비어있습니다.")
    idx = max(1, int(rule_index)) - 1
    if idx >= len(seq):
        idx = len(seq) - 1
    rule = seq[idx]
    rule["__name"] = rule.get("name") or f"rule-{rule_index}"
    return rule


def _ensure_fields_in_df(df: pd.DataFrame) -> pd.DataFrame:
    # 필요한 파생 컬럼이 없으면 즉석 생성(엑셀에서 build_index를 거치지 않은 경우 대비)
    need = ["building_unit_price","land_unit_price","total_appraisal_price"]
    missing = [c for c in need if c not in df.columns]
    if missing:
        derived = []
        for _, row in df.iterrows():
            derived.append(derive_fields(dict(row)))
        ddf = pd.DataFrame(derived)
        for c in need:
            if c not in df.columns:
                df[c] = ddf[c]
    return df


def _filter_time_window(df: pd.DataFrame, days: int) -> pd.DataFrame:
    if not days:
        return df
    today = _today_days()
    lo = today - int(days)
    hi = today
    if "auction_days" not in df.columns:
        return df
    return df.loc[(df["auction_days"] >= lo) & (df["auction_days"] <= hi)]


def _filter_radius(df: pd.DataFrame, subj: Dict[str, Any], idx: InMemoryIndex, radius_m: int) -> pd.DataFrame:
    if not radius_m:
        return df
    lat, lon = subj.get("lat"), subj.get("lon")
    if lat is None or lon is None or idx.lats is None or idx.lons is None:
        # 좌표가 없을 때 텍스트 기반 폴백: 동/구/도시 매칭으로 근사 반경 적용
        import re as _re
        s_city = (subj.get("city") or "").strip()
        s_gu = (subj.get("gu") or "").strip()
        s_dong = (subj.get("dong") or "").strip()
        addr = df.get("address")
        if addr is None:
            return df
        ser = addr.astype(str).fillna("")
        def _mask_contains(parts):
            if not parts:
                return None
            lookaheads = "".join([f"(?=.*{_re.escape(p)})" for p in parts if p])
            if not lookaheads:
                return None
            rx = _re.compile(lookaheads)
            return ser.str.contains(rx)
        # 1km 이내 → city+gu+dong 모두 동일로 근사
        if radius_m <= 1200 and s_city and s_gu and s_dong:
            m = _mask_contains([s_city, s_gu, s_dong])
            if m is not None:
                return df.loc[m]
        # 5km/10km 수준 → city+gu 동일로 근사
        if radius_m <= 12000 and s_city and s_gu:
            m = _mask_contains([s_city, s_gu])
            if m is not None:
                return df.loc[m]
        # 더 넓은 반경 → city 동일 정도로 근사
        if s_city:
            m = _mask_contains([s_city])
            if m is not None:
                return df.loc[m]
        return df
    # 캐시된 거리 사용 후 마스크 적용
    try:
        la1 = float(lat); lo1 = float(lon)
        dist_cache = subj.get("__idx_dist_cache")
        key = subj.get("__idx_dist_cache_key")
        if dist_cache is not None and key == (round(la1, 6), round(lo1, 6)):
            labels = df.index.to_numpy()
            dsel = dist_cache[labels]
            return df.loc[dsel <= float(radius_m)]
    except Exception:
        pass
    ring = radius_candidates(idx, lat, lon, float(radius_m))
    if ring.size == 0:
        return df.iloc[0:0]
    # ring은 idx.df(리셋된 0..N-1)의 위치 인덱스. df는 필터를 거친 서브셋이라 위치기반(iloc) 대신
    # 라벨기반(loc) 교집합으로 선택해야 정확합니다.
    import numpy as _np
    labels = df.index.to_numpy()
    keep = _np.intersect1d(labels, ring, assume_unique=False)
    df2 = df.loc[keep]
    # 추가 안전망: 벡터화 거리 재검증(좌표가 있는 경우)
    try:
        la1 = float(subj.get("lat")) if subj.get("lat") is not None else None
        lo1 = float(subj.get("lon")) if subj.get("lon") is not None else None
        if la1 is not None and lo1 is not None and ("lat" in df2.columns) and ("lon" in df2.columns):
            la2 = df2["lat"].to_numpy(dtype=float, copy=False)
            lo2 = df2["lon"].to_numpy(dtype=float, copy=False)
            valid = (~_np.isnan(la2)) & (~_np.isnan(lo2))
            la1r = _np.deg2rad(la1)
            lo1r = _np.deg2rad(lo1)
            la2r = _np.deg2rad(la2)
            lo2r = _np.deg2rad(lo2)
            dlat = la2r - la1r
            dlon = lo2r - lo1r
            a = _np.sin(dlat/2.0)**2 + _np.cos(la1r)*_np.cos(la2r)*_np.sin(dlon/2.0)**2
            c = 2.0 * _np.arcsin(_np.sqrt(a))
            d = 6371000.0 * c
            mask = valid & (d <= float(radius_m))
            df2 = df2.loc[mask]
    except Exception:
        pass
    return df2


def _filter_same_apartment(df: pd.DataFrame, subj: Dict[str, Any]) -> pd.DataFrame:
    # 주소의 필지번호(예: ... 동 724 또는 724-1)까지 동일한지 비교
    saddr = (subj.get("address") or "").strip()
    if not saddr:
        return df.iloc[0:0]
    import re as _re
    def _prefix_up_to_lot(text: object) -> str:
        try:
            s = " ".join(str(text or "").split())
        except Exception:
            return ""
        m = _re.search(r"(\d{1,6}(?:-\d{1,6})?)", s)
        if m:
            return s[: m.end()].strip()
        return s
    sbase = _prefix_up_to_lot(saddr)
    addrs = df.get("address")
    if addrs is None:
        return df.iloc[0:0]
    bases = addrs.astype(str).map(_prefix_up_to_lot)
    return df.loc[bases == sbase]


def _filter_same_building(df: pd.DataFrame, subj: Dict[str, Any]) -> pd.DataFrame:
    # 현재는 _filter_same_apartment을 공용
    return _filter_same_apartment(df, subj)


def _filter_value_ranges(df: pd.DataFrame, subj: Dict[str, Any], filters: Dict[str, float]) -> pd.DataFrame:
    if not filters:
        return df
    out = df
    # center 값은 subject의 파생 값에서 가져옴
    svals = derive_fields(subj)
    for key, pct in filters.items():
        if key == "building_area_pct":
            # 면적 비교를 '평' 기준으로 수행
            PYEONG = 3.305785
            # 주제(center): 우선 평 컬럼, 없으면 m^2 → 평 변환
            center_py = subj.get("area_building")
            if center_py in (None, 0):
                center_m2 = subj.get("area_building_m")
                if center_m2 is None:
                    center_m2 = svals.get("building_area")
                center_py = (float(center_m2) / PYEONG) if center_m2 not in (None, 0) else None
            if center_py in (None, 0):
                return out.iloc[0:0]
            # 후보: 평 시리즈 구성 (area_building 있으면 사용, 없으면 m^2 → 평 변환)
            cand_py = None
            if "area_building" in out.columns:
                cand_py = pd.to_numeric(out["area_building"], errors="coerce")
            elif "building_area" in out.columns:
                cand_py = pd.to_numeric(out["building_area"], errors="coerce") / PYEONG
            elif "building_area_m" in out.columns:
                cand_py = pd.to_numeric(out["building_area_m"], errors="coerce") / PYEONG
            else:
                return out.iloc[0:0]
            mask = cand_py.apply(lambda v: _within_pct(center_py, v, float(pct)))
            out = out.loc[mask]
        elif key == "building_unit_price_pct":
            # 건물 평당 감정가 필터링:
            #  - 주제(center): 데이터디스크에서 온 building_unit_price
            #  - 후보: auction_constructed.xlsx 의 building_unit_price
            col, center = "building_unit_price", svals.get("building_unit_price")
        elif key == "total_appraisal_price_pct":
            # 총감정가 필터링:
            #  - 주제(center): 데이터디스크에서 온 total_appraisal_price
            #  - 후보: auction_constructed.xlsx 의 appraisal_price
            col = "appraisal_price"
            center = svals.get("total_appraisal_price")
            # 디버그: 허용 범위 및 필터 전후 개수 출력
            try:
                c_val = float(center)
                pct_f = float(pct)
                lo = c_val * (1.0 - pct_f)
                hi = c_val * (1.0 + pct_f)
                print(
                    f"[debug total_app] center={c_val:.0f}, pct={pct_f}, "
                    f"range=({lo:.0f}, {hi:.0f}), before={len(out)}"
                )
            except Exception:
                print(f"[debug total_app] center={center}, pct={pct}, before={len(out)}")
        elif key == "land_area_pct":
            # 면적 비교를 '평' 기준으로 수행
            PYEONG = 3.305785
            # 주제(center): 우선 평 컬럼, 없으면 m^2 → 평 변환
            center_py = subj.get("area_land")
            if center_py in (None, 0):
                center_m2 = subj.get("area_land_m")
                if center_m2 is None:
                    center_m2 = svals.get("land_area")
                center_py = (float(center_m2) / PYEONG) if center_m2 not in (None, 0) else None
            if center_py in (None, 0):
                return out.iloc[0:0]
            # 후보: 평 시리즈 구성 (area_land 있으면 사용, 없으면 m^2 → 평 변환)
            cand_py = None
            if "area_land" in out.columns:
                cand_py = pd.to_numeric(out["area_land"], errors="coerce")
            elif "land_area" in out.columns:
                cand_py = pd.to_numeric(out["land_area"], errors="coerce") / PYEONG
            elif "land_area_m" in out.columns:
                cand_py = pd.to_numeric(out["land_area_m"], errors="coerce") / PYEONG
            else:
                return out.iloc[0:0]
            mask = cand_py.apply(lambda v: _within_pct(center_py, v, float(pct)))
            out = out.loc[mask]
        else:
            continue

        # building_unit_price_pct / total_appraisal_price_pct 등
        # 공통: center/필터가 적용된 이후 남은 개수 디버그 출력
        if key == "total_appraisal_price_pct":
            # 실제 필터 적용 (위에서 col/center 설정됨)
            if center in (None, 0) or col not in out.columns:
                print("[debug total_app] skip: center is None/0 or column missing")
                return out.iloc[0:0]
            before = len(out)
            out = out.loc[out[col].apply(lambda v: _within_pct(center, v, float(pct)))]
            print(f"[debug total_app] after filter: {len(out)} (removed {before - len(out)})")
        elif key == "building_unit_price_pct":
            if center in (None, 0) or col not in out.columns:
                print("[debug bu_price] skip: center is None/0 or column missing")
                return out.iloc[0:0]
            before = len(out)
            out = out.loc[out[col].apply(lambda v: _within_pct(center, v, float(pct)))]
            print(f"[debug bu_price] center={center}, pct={pct}, after={len(out)} (removed {before - len(out)})")
        elif key != "building_area_pct":  # building_area_pct는 이미 위에서 처리
            print(f"[debug filter] key={key}, after={len(out)}")
    return out


def _sort_recency_then_distance(df: pd.DataFrame, subj: Dict[str, Any]) -> pd.DataFrame:
    # recency desc, distance asc
    import numpy as _np
    # 가급적 auction_days 기준
    if "auction_days" in df.columns:
        days = df["auction_days"].fillna(-1e15)
    else:
        days = -1
    # 거리 계산 존재 시 사용
    if ("lat" in df.columns) and ("lon" in df.columns):
        try:
            la1 = float(subj.get("lat")) if subj.get("lat") is not None else None
            lo1 = float(subj.get("lon")) if subj.get("lon") is not None else None
        except Exception:
            la1, lo1 = None, None
        if la1 is None or lo1 is None:
            return df.sort_values(["auction_days"], ascending=[False])
        # 캐시된 거리 사용(있으면)
        d = None
        try:
            cache = subj.get("__idx_dist_cache")
            key = subj.get("__idx_dist_cache_key")
            if cache is not None and key == (round(la1, 6), round(lo1, 6)):
                labels = df.index.to_numpy()
                d = cache[labels]
        except Exception:
            d = None
        if d is None:
            la2 = _np.deg2rad(df["lat"].to_numpy(dtype=float, copy=False))
            lo2 = _np.deg2rad(df["lon"].to_numpy(dtype=float, copy=False))
            valid = (~_np.isnan(la2)) & (~_np.isnan(lo2))
            la1r = _np.deg2rad(la1)
            lo1r = _np.deg2rad(lo1)
            dlat = la2 - la1r
            dlon = lo2 - lo1r
            a = _np.sin(dlat/2.0)**2 + _np.cos(la1r)*_np.cos(la2)*_np.sin(dlon/2.0)**2
            c = 2.0 * _np.arcsin(_np.sqrt(a))
            d = 6371000.0 * c
            d[~valid] = 9_99_99_99
        df = df.copy()
        df["__dist"] = d
        return df.sort_values(["auction_days","__dist"], ascending=[False, True]).drop(columns=["__dist"], errors="ignore")
    return df.sort_values(["auction_days"], ascending=[False])


def recommend_by_rule(subject_row: Dict[str, Any], idx: InMemoryIndex, cfg: Dict[str, Any],
                     rule_index: int = 1, similar_land: bool = False,
                     category_override: str = None, rule_index_map: Dict[str, int] | None = None,
                     topk: int = 10, region_scope: str = "big") -> List[Dict[str, Any]]:
    subj = enrich_record_minimal(subject_row)
    # 디버그 단계 로그는 기본 비활성화 (환경변수와 상관없이 출력 안 함)
    _dbg_on = False
    def _dbg(label: str, df_len: int) -> None:
        if _dbg_on:
            try:
                print(f"[dbg] {label}: {df_len}")
            except Exception:
                pass
    # 카테고리 결정
    cat = category_override or category_from_usage(subj.get("usage") or "", similar_land)
    # 3) 주거/커뮤니티군 유사토지: 카테고리 5의 land_like 로직을 따르도록 강제
    if similar_land:
        utext = str(subj.get("usage") or "")
        _res_comm_keys = [
            "주택","다가구","근린주택","근린시설","숙박시설","숙박(콘도등)",
            "교육시설","종교시설","의료시설","목욕탕","노유자시설","장례관련시설","문화및집회시설",
        ]
        if any(k in utext for k in _res_comm_keys):
            cat = _CAT5_OTHER_BIG
    _idx_to_use = rule_index
    if rule_index_map and cat in rule_index_map:
        try:
            _idx_to_use = int(rule_index_map[cat])
        except Exception:
            pass
    rule = _load_rule(cfg, cat, _idx_to_use, similar_land)

    df = _ensure_fields_in_df(idx.df)
    _dbg("init", len(df))

    # 거리 캐시 준비(주제 기준) - 순위 간 재사용
    try:
        la1 = float(subj.get("lat")) if subj.get("lat") is not None else None
        lo1 = float(subj.get("lon")) if subj.get("lon") is not None else None
    except Exception:
        la1, lo1 = None, None
    if la1 is not None and lo1 is not None and (idx.lats is not None) and (idx.lons is not None):
        import numpy as _np
        try:
            key = (round(la1, 6), round(lo1, 6))
            need_build = True
            # 이전 캐시와 동일하면 스킵
            if hasattr(recommend_by_rule, "_cache_key") and hasattr(recommend_by_rule, "_cache_dist"):
                if recommend_by_rule._cache_key == key and len(getattr(recommend_by_rule, "_cache_dist", [])) == len(idx.df):
                    need_build = False
            if need_build:
                la2 = _np.deg2rad(idx.lats.astype(float, copy=False))
                lo2 = _np.deg2rad(idx.lons.astype(float, copy=False))
                valid = (~_np.isnan(la2)) & (~_np.isnan(lo2))
                la1r = _np.deg2rad(la1)
                lo1r = _np.deg2rad(lo1)
                dlat = la2 - la1r
                dlon = lo2 - lo1r
                a = _np.sin(dlat/2.0)**2 + _np.cos(la1r)*_np.cos(la2)*_np.sin(dlon/2.0)**2
                c = 2.0 * _np.arcsin(_np.sqrt(a))
                dist_all = 6371000.0 * c
                dist_all[~valid] = 9_99_99_99
                recommend_by_rule._cache_key = key
                recommend_by_rule._cache_dist = dist_all
            # 서브루틴으로 캐시 전달(정렬/반경에서 사용)
            subj["__idx_dist_cache"] = recommend_by_rule._cache_dist
            subj["__idx_dist_cache_key"] = recommend_by_rule._cache_key
        except Exception:
            pass

    # 0) 지역 필터
    #   - 항상 region_big(특별/광역시/도) 일치 필터 먼저 적용
    #   - region_scope == "mid" 인 경우에는 region_mid(시/군/구)까지 일치시키는 추가 필터 적용
    def _norm_province(v: Any) -> str:
        """대지역 명칭 정규화.

        - '강원특별자치도' → '강원도' 처럼 '특별자치도' 접미어를 '도'로 축약
        - 공백 제거 후 비교
        """
        try:
            s = str(v or "").strip()
        except Exception:
            return ""
        if not s:
            return ""
        s_compact = s.replace(" ", "")
        if s_compact.endswith("특별자치도"):
            s_compact = s_compact.replace("특별자치도", "도")
        return s_compact

    try:
        sbig = subj.get("region_big")
    except Exception:
        sbig = ""
    sbig_norm = _norm_province(sbig)
    if sbig_norm and ("region_big" in df.columns):
        df = df.loc[df["region_big"].apply(_norm_province) == sbig_norm]
    _dbg("after_region_big", len(df))

    if str(region_scope).lower() == "mid":
        try:
            smid = (subj.get("region_mid") or "").strip()
        except Exception:
            smid = ""
        if smid and ("region_mid" in df.columns):
            _norm_mid = lambda x: str(x or "").strip()
            df = df.loc[df["region_mid"].astype(str).map(_norm_mid) == _norm_mid(smid)]
        else:
            # 중지역 정보가 없으면 mid 스코프에서는 추천하지 않음
            df = df.iloc[0:0]
        _dbg("after_region_mid", len(df))

    # usage 기본 스코프: 동일 usage만 비교 + (아래 예외 매핑만 허용)
    # similar_land=True 인 경우, 특정 용도는 "시설"이 아닌 "토지용도"만 대상으로 제한
    def _allowed_usages(usage_text: str) -> List[str]:
        u = (usage_text or "").strip()
        if not u:
            return []
        # 1) 디폴트: 동일 usage만 비교
        allow = [u]
        # 2) 예외 매핑(정확 일치만 허용)
        exception_map = {
            "사무실": ["사무실", "근린상가"],
            "공장": ["공장", "창고"],
            "창고": ["공장", "창고"],
            "농가관련시설": ["농가관련시설", "창고"],
            "대지": ["대지", "잡종지"],
            "잡종지": ["잡종지", "대지"],
            "목장용지": ["목장용지", "과수원", "농지"],
            "공장용지": ["공장용지", "창고용지", "잡종지"],
            "학교용지": ["학교용지", "잡종지"],
            "창고용지": ["창고용지", "잡종지"],
            "체육용지": ["체육용지", "잡종지"],
            "종교용지": ["종교용지", "잡종지"],
            "기타용지": ["기타용지", "잡종지"],
        }
        if u in exception_map:
            allow = exception_map[u]
        # 3) 유사토지 옵션
        if similar_land:
            # 13) 산업/위험물/환경/차량 관련
            #   - 유사토지 모드에서는 "공장/창고 같은 시설"은 제외하고
            #   - 오직 "공장용지, 창고용지" 토지용도만 대상으로 한다.
            #   - 공장 usage도 여기서 함께 처리한다.
            _fac_industrial = ["공장", "창고", "농장", "농가관련시설", "주유소(위험물)", "분뇨쓰레기처리", "자동차관련시설"]
            if u in _fac_industrial:
                allow = ["공장용지", "창고용지"]
            # 14) 주거/커뮤니티군
            #   - 유사토지 모드에서는 "주택/단독주택/근린시설 등 건물"은 제외하고
            #   - 오직 "대지, 잡종지" 토지만 대상으로 한다.
            _fac_res_comm = [
                "주택","단독주택","다가구","근린주택","근린시설","숙박시설","숙박(콘도등)",
                "교육시설","종교시설","의료시설","목욕탕","노유자시설","장례관련시설","문화및집회시설",
            ]
            if u in _fac_res_comm:
                allow = ["대지", "잡종지"]
        return allow

    su = str(subj.get("usage") or "").strip()
    allowed = set(_allowed_usages(su))
    if allowed:
        df = df.loc[df["usage"].astype(str).isin(allowed)]
    _dbg("after_allowed_usage", len(df))

    # same_facility가 명시된 경우에는 동일 용도 강제(상위 허용집합이 더 넓어도 좁힘)
    same_facility = bool(rule.get("same_facility", False))
    if same_facility and su:
        df = df.loc[df["usage"].astype(str).str.strip() == su.strip()]
    _dbg("after_same_facility", len(df))

    # 시간창
    tw = int(rule.get("time_window_days" , 0) or 0)
    if tw:
        df = _filter_time_window(df, tw)
    _dbg("after_time_window", len(df))

    # 동일 단지/건물
    if rule.get("require_same_apartment"):
        df = _filter_same_apartment(df, subj)
    if rule.get("require_same_building"):
        df = _filter_same_building(df, subj)
    _dbg("after_same_building_apartment", len(df))

    # 값 범위 필터
    df = _filter_value_ranges(df, subj, rule.get("filters") or {})
    _dbg("after_value_filters", len(df))

    # 반경(마지막 단계)
    rad = int(rule.get("radius_m", 0) or 0)
    if rad:
        df = _filter_radius(df, subj, idx, rad)
    _dbg("after_radius", len(df))

    if df.empty:
        return []
    # 정렬 및 상위 k
    df = _sort_recency_then_distance(df, subj)
    out: List[Dict[str, Any]] = []
    for _, row in df.head(topk).iterrows():
        r = row.to_dict()
        out.append({
            "case_no": r.get("case_no"),
            "score": None,
            "detail": {"rule": rule.get("__name"), "category": cat},
            "candidate": r,
        })
    return out
