# -*- coding: utf-8 -*-
"""
recommend.py

규칙 기반 유사물건 추천 엔진
- 대상 물건의 카테고리에 따라 적합한 규칙 선택
- 규칙에 따라 후보군 필터링
- 최신 + 가까운 순으로 정렬하여 topk 반환
"""

import os
import re
from datetime import datetime
from typing import Any, Dict, List, Optional

import pandas as pd
import yaml

from .utils import (
    category_from_usage,
    derive_fields,
    haversine_distance_m,
    safe_float,
    to_days_from_epoch,
    within_pct,
    ensure_derived_columns,
    ensure_auction_days,
    CAT_PLANT_WAREHOUSE_ETC,
    CAT_OTHER_BIG,
)


# -----------------------
# 설정 로드
# -----------------------
def load_config(config_path: Optional[str] = None) -> Dict[str, Any]:
    """YAML 설정 파일 로드."""
    if config_path is None:
        config_path = os.path.join(os.path.dirname(__file__), "config.yaml")
    with open(config_path, "r", encoding="utf-8") as f:
        return yaml.safe_load(f)


def get_rules_for_category(
    cfg: Dict[str, Any],
    category: str,
    similar_land: bool = False,
) -> List[Dict[str, Any]]:
    """카테고리에 맞는 규칙 리스트 반환."""
    rules_all = cfg.get("rules", {})
    cat_rules = rules_all.get(category)
    if cat_rules is None:
        return []

    # PLANT_WAREHOUSE_ETC, OTHER_BIG은 default/land_like 하위 구조
    if category in (CAT_PLANT_WAREHOUSE_ETC, CAT_OTHER_BIG):
        key = "land_like" if similar_land else "default"
        return cat_rules.get(key, [])

    return cat_rules


def get_rule_count(cfg: Dict[str, Any], category: str, similar_land: bool = False) -> int:
    """해당 카테고리의 규칙 개수 반환."""
    rules = get_rules_for_category(cfg, category, similar_land)
    return len(rules)


# -----------------------
# 아파트/건물명 추출
# -----------------------
def extract_apt_name(address: str) -> Optional[str]:
    """주소에서 아파트 단지명 추출 (예: '○○아파트', '○○힐스테이트')."""
    if not address:
        return None
    # 패턴: 공백 뒤에 아파트/○○아파트/○○힐스테이트 등
    patterns = [
        r"([가-힣A-Za-z0-9]+아파트)",
        r"([가-힣A-Za-z0-9]+힐스테이트)",
        r"([가-힣A-Za-z0-9]+푸르지오)",
        r"([가-힣A-Za-z0-9]+자이)",
        r"([가-힣A-Za-z0-9]+래미안)",
        r"([가-힣A-Za-z0-9]+e편한세상)",
    ]
    for pat in patterns:
        m = re.search(pat, address)
        if m:
            return m.group(1)
    return None


def extract_building_base(address: str) -> Optional[str]:
    """주소에서 건물 기준 문자열 추출 (동/호 제외)."""
    if not address:
        return None
    # 동/호 제거: '101동 502호' 등
    cleaned = re.sub(r"\d+동\s*\d*호?", "", address).strip()
    return cleaned if cleaned else None


# -----------------------
# 필터링 함수들
# -----------------------
def filter_by_region(df: pd.DataFrame, subj: Dict[str, Any], scope: str = "big") -> pd.DataFrame:
    """지역 필터링 (scope: big=시도, mid=시군구)."""
    if scope == "big":
        region_big = subj.get("region_big")
        if region_big:
            return df[df["region_big"] == region_big]
    elif scope == "mid":
        region_big = subj.get("region_big")
        region_mid = subj.get("region_mid")
        if region_big and region_mid:
            return df[(df["region_big"] == region_big) & (df["region_mid"] == region_mid)]
    return df


def filter_by_usage(df: pd.DataFrame, subj: Dict[str, Any]) -> pd.DataFrame:
    """동일 용도만 필터링."""
    usage = subj.get("usage")
    if usage:
        return df[df["usage"] == usage]
    return df


def filter_by_time_window(df: pd.DataFrame, subj: Dict[str, Any], days: int) -> pd.DataFrame:
    """시간 윈도우 필터링 (낙찰일 기준)."""
    subj_days = subj.get("auction_days")
    if subj_days is None:
        subj_days = to_days_from_epoch(subj.get("auction_date"))
    if subj_days is None:
        # 기준일이 없으면 오늘 기준
        subj_days = to_days_from_epoch(datetime.now())

    if "auction_days" in df.columns:
        return df[(df["auction_days"] >= subj_days - days) & (df["auction_days"] <= subj_days)]
    return df


def filter_by_radius(df: pd.DataFrame, subj: Dict[str, Any], radius_m: float) -> pd.DataFrame:
    """거리 반경 필터링."""
    if radius_m <= 0:
        return df
    lat, lon = safe_float(subj.get("latitude")), safe_float(subj.get("longitude"))
    if lat is None or lon is None:
        return df

    def in_radius(row):
        r_lat, r_lon = safe_float(row.get("latitude")), safe_float(row.get("longitude"))
        dist = haversine_distance_m(lat, lon, r_lat, r_lon)
        return dist is not None and dist <= radius_m

    mask = df.apply(in_radius, axis=1)
    return df[mask]


def filter_by_same_apartment(df: pd.DataFrame, subj: Dict[str, Any]) -> pd.DataFrame:
    """동일 아파트 단지 필터링."""
    apt_name = extract_apt_name(subj.get("address", ""))
    if not apt_name:
        return df

    def same_apt(row):
        row_apt = extract_apt_name(row.get("address", ""))
        return row_apt == apt_name

    mask = df.apply(same_apt, axis=1)
    return df[mask]


def filter_by_same_building(df: pd.DataFrame, subj: Dict[str, Any]) -> pd.DataFrame:
    """동일 건물 필터링 (상가/사무실용)."""
    building_base = extract_building_base(subj.get("address", ""))
    if not building_base:
        return df

    def same_building(row):
        row_base = extract_building_base(row.get("address", ""))
        return row_base == building_base

    mask = df.apply(same_building, axis=1)
    return df[mask]


def filter_by_value_range(df: pd.DataFrame, subj: Dict[str, Any], filters: Dict[str, float]) -> pd.DataFrame:
    """값 범위 필터링 (면적, 단가, 감정가 등)."""
    if not filters:
        return df

    for key, pct in filters.items():
        # key 형식: building_area_pct -> building_area
        col = key.replace("_pct", "")
        subj_val = safe_float(subj.get(col))
        if subj_val is None:
            continue

        def in_range(row, c=col, sv=subj_val, p=pct):
            rv = safe_float(row.get(c))
            return rv is not None and within_pct(sv, rv, p)

        mask = df.apply(in_range, axis=1)
        df = df[mask]

    return df


# -----------------------
# 정렬
# -----------------------
def sort_by_recency_then_distance(df: pd.DataFrame, subj: Dict[str, Any]) -> pd.DataFrame:
    """최신순 + 가까운 순 정렬."""
    lat, lon = safe_float(subj.get("latitude")), safe_float(subj.get("longitude"))

    # 거리 계산
    def calc_dist(row):
        r_lat, r_lon = safe_float(row.get("latitude")), safe_float(row.get("longitude"))
        return haversine_distance_m(lat, lon, r_lat, r_lon) or float("inf")

    df = df.copy()
    df["_distance"] = df.apply(calc_dist, axis=1)

    # auction_days 내림차순 (최신), 거리 오름차순
    sort_cols = []
    sort_asc = []
    if "auction_days" in df.columns:
        sort_cols.append("auction_days")
        sort_asc.append(False)  # 내림차순 (최신)
    sort_cols.append("_distance")
    sort_asc.append(True)  # 오름차순 (가까운)

    df = df.sort_values(sort_cols, ascending=sort_asc)
    df = df.drop(columns=["_distance"])
    return df


# -----------------------
# 메인 추천 함수
# -----------------------
def recommend_by_rule(
    subject: Dict[str, Any],
    candidates_df: pd.DataFrame,
    cfg: Dict[str, Any],
    rule_index: int = 1,
    similar_land: bool = False,
    category_override: Optional[str] = None,
    region_scope: str = "big",
    topk: int = 10,
) -> List[Dict[str, Any]]:
    """
    규칙 기반 유사물건 추천 수행.

    Args:
        subject: 대상 물건 정보 dict
        candidates_df: 후보군 DataFrame (auction_cases 테이블)
        cfg: 설정 dict (load_config()로 로드)
        rule_index: 적용할 규칙 순번 (1-based)
        similar_land: 토지 유사 모드 (PLANT_WAREHOUSE_ETC, OTHER_BIG용)
        category_override: 카테고리 수동 지정
        region_scope: 지역 범위 ("big"=시도, "mid"=시군구)
        topk: 반환할 최대 건수

    Returns:
        추천 결과 리스트 (dict)
    """
    # 1. 대상 물건 보강 (파생값 계산)
    subj = {**subject}
    derived = derive_fields(subj)
    for k, v in derived.items():
        if subj.get(k) is None:
            subj[k] = v
    if subj.get("auction_days") is None:
        subj["auction_days"] = to_days_from_epoch(subj.get("auction_date"))

    # 2. 카테고리 결정
    category = category_override or category_from_usage(subj.get("usage", ""), similar_land)

    # 3. 규칙 로드
    rules = get_rules_for_category(cfg, category, similar_land)
    if not rules:
        return []

    if rule_index < 1 or rule_index > len(rules):
        return []

    rule = rules[rule_index - 1]

    # 4. 후보군 전처리
    df = candidates_df.copy()
    df = ensure_derived_columns(df)
    df = ensure_auction_days(df)

    # 5. 필터링 적용
    # 5.1. 지역 필터
    df = filter_by_region(df, subj, scope=region_scope)

    # 5.2. 용도 필터 (동일 용도만)
    df = filter_by_usage(df, subj)

    # 5.3. 시간 윈도우
    time_window = rule.get("time_window_days")
    if time_window:
        df = filter_by_time_window(df, subj, time_window)

    # 5.4. 동일 아파트/건물
    if rule.get("require_same_apartment"):
        df = filter_by_same_apartment(df, subj)
    if rule.get("require_same_building"):
        df = filter_by_same_building(df, subj)

    # 5.5. 값 범위 필터
    filters = rule.get("filters", {})
    df = filter_by_value_range(df, subj, filters)

    # 5.6. 거리 반경
    radius_m = rule.get("radius_m", 0)
    if radius_m > 0:
        df = filter_by_radius(df, subj, radius_m)

    # 6. 정렬
    df = sort_by_recency_then_distance(df, subj)

    # 7. topk 반환
    results = df.head(topk).to_dict(orient="records")

    # 결과에 메타 정보 추가
    for r in results:
        r["_rule_name"] = rule.get("name")
        r["_rule_index"] = rule_index
        r["_category"] = category

    return results


def recommend_all_rules(
    subject: Dict[str, Any],
    candidates_df: pd.DataFrame,
    cfg: Dict[str, Any],
    similar_land: bool = False,
    category_override: Optional[str] = None,
    region_scope: str = "big",
    topk: int = 10,
) -> Dict[int, List[Dict[str, Any]]]:
    """
    모든 규칙에 대해 추천 수행.

    Returns:
        {rule_index: [추천결과]} dict
    """
    category = category_override or category_from_usage(subject.get("usage", ""), similar_land)
    rule_count = get_rule_count(cfg, category, similar_land)

    results = {}
    for idx in range(1, rule_count + 1):
        res = recommend_by_rule(
            subject,
            candidates_df,
            cfg,
            rule_index=idx,
            similar_land=similar_land,
            category_override=category_override,
            region_scope=region_scope,
            topk=topk,
        )
        results[idx] = res

    return results

