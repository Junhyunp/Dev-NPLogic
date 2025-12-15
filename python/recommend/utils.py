# -*- coding: utf-8 -*-
"""
utils.py

유사물건 추천에서 사용하는 공통 유틸리티
- 날짜/숫자 파싱, 거리 계산
- 파생값 계산 (평당 단가, 총감정가)
- 카테고리 판별
"""

import math
from datetime import datetime
from typing import Any, Dict, Optional

import numpy as np
import pandas as pd


# -----------------------
# 공통 상수
# -----------------------
EPOCH = datetime(1970, 1, 1)
PYEONG = 3.305785  # 1평 = 3.305785㎡


# -----------------------
# 날짜/숫자 파싱
# -----------------------
def parse_date(s: Any) -> Optional[datetime]:
    """넓은 입력을 datetime으로 파싱."""
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


# -----------------------
# 거리 계산
# -----------------------
def haversine_distance_m(lat1, lon1, lat2, lon2) -> Optional[float]:
    """위경도 간 하버사인 거리(미터)."""
    try:
        if None in (lat1, lon1, lat2, lon2):
            return None
        R = 6371000.0
        p1, p2 = math.radians(float(lat1)), math.radians(float(lat2))
        dphi = math.radians(float(lat2) - float(lat1))
        dlmb = math.radians(float(lon2) - float(lon1))
        a = math.sin(dphi / 2) ** 2 + math.cos(p1) * math.cos(p2) * math.sin(dlmb / 2) ** 2
        c = 2 * math.atan2(math.sqrt(a), math.sqrt(1 - a))
        return R * c
    except Exception:
        return None


# -----------------------
# 파생값 계산
# -----------------------
def derive_fields(row: Dict[str, Any]) -> Dict[str, Optional[float]]:
    """단가/총감정가/면적 파생값 계산.

    - 건물/토지 단가: 평 단가 기준
      building_unit_price = building_appraisal_price / (building_area / PYEONG)
      land_unit_price     = land_appraisal_price / (land_area / PYEONG)
    - 총감정가: 건물+토지 합
    """
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

    b_unit = (
        (float(b_app) / float(b_area_py))
        if (b_app not in (None, 0) and b_area_py not in (None, 0))
        else None
    )
    l_unit = (
        (float(l_app) / float(l_area_py))
        if (l_app not in (None, 0) and l_area_py not in (None, 0))
        else None
    )

    return {
        "building_unit_price": b_unit,
        "land_unit_price": l_unit,
        "total_appraisal_price": total_app,
        "building_area": b_area_m2,
        "land_area": l_area_m2,
    }


# -----------------------
# 카테고리 판별
# -----------------------
CAT_APT_OFFICETEL = "APT_OFFICETEL"
CAT_ROWHOUSE_MULTI = "ROWHOUSE_MULTI"
CAT_RETAIL_OFFICE_APT_FACTORY = "RETAIL_OFFICE_APT_FACTORY"
CAT_PLANT_WAREHOUSE_ETC = "PLANT_WAREHOUSE_ETC"
CAT_OTHER_BIG = "OTHER_BIG"


def category_from_usage(usage_raw: Any, similar_land: bool = False) -> str:
    """물건 용도(usage)에서 카테고리를 판별."""
    u = str(usage_raw or "")

    # 1) 아파트형공장 우선 처리
    if "아파트형공장" in u:
        return CAT_RETAIL_OFFICE_APT_FACTORY

    # 2) 아파트/오피스텔
    if ("아파트" in u) or ("오피스텔" in u):
        return CAT_APT_OFFICETEL

    # 3) 연립/다세대
    if ("연립" in u) or ("다세대" in u):
        return CAT_ROWHOUSE_MULTI

    # 4) 근린상가/사무실 + 기타 시설
    if any(
        k in u
        for k in [
            "근린상가",
            "상가",
            "사무실",
            "숙박시설",
            "숙박(콘도등)",
            "교육시설",
            "종교시설",
            "의료시설",
            "목욕탕",
            "노유자시설",
            "문화및집회시설",
        ]
    ):
        return CAT_RETAIL_OFFICE_APT_FACTORY

    # 5) 창고/공장/농가 등 산업군
    if any(
        k in u
        for k in [
            "공장",
            "창고",
            "농가관련시설",
            "주유소(위험물)",
            "분뇨",
            "쓰레기",
            "자동차관련시설",
        ]
    ):
        return CAT_PLANT_WAREHOUSE_ETC

    # 6) 그 외
    return CAT_OTHER_BIG


# -----------------------
# 범위 체크
# -----------------------
def within_pct(center: Any, value: Any, pct: float) -> bool:
    """value가 center의 ±pct% 범위 내인지 확인."""
    try:
        c = float(center)
        v = float(value)
    except Exception:
        return False
    if pct is None:
        return True
    lo, hi = c * (1 - pct), c * (1 + pct)
    return (v >= lo) and (v <= hi)


# -----------------------
# DataFrame 보강
# -----------------------
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


def ensure_auction_days(df: pd.DataFrame) -> pd.DataFrame:
    """auction_days 컬럼이 없으면 auction_date에서 파생."""
    if "auction_days" not in df.columns and "auction_date" in df.columns:
        df = df.copy()
        df["auction_days"] = df["auction_date"].apply(to_days_from_epoch)
    return df

