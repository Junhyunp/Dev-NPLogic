# -*- coding: utf-8 -*-
from __future__ import annotations

import argparse
import os
from typing import List, Tuple, Optional

import pandas as pd


def _read_excel(path: str, sheet: int = 0) -> pd.DataFrame:
    if not os.path.isfile(path):
        raise FileNotFoundError(path)
    return pd.read_excel(path, sheet_name=sheet)


def _norm_str(x: object) -> str:
    if x is None:
        return ""
    try:
        if pd.isna(x):
            return ""
    except Exception:
        pass
    return str(x).strip().lower()


def _select_merge_keys(
    base_cols: List[str],
    delta_cols: List[str],
    explicit_keys: Optional[List[str]] = None,
) -> List[str]:
    """
    두 데이터프레임 모두에 존재하는 병합키 목록을 결정한다.
    우선순위: explicit_keys → popup_url → (auctionone_court, auctionone_area, case_no) → case_no
    """
    if explicit_keys:
        if all(k in base_cols for k in explicit_keys) and all(k in delta_cols for k in explicit_keys):
            return list(explicit_keys)
        else:
            raise KeyError(f"명시한 키가 컬럼에 없습니다: {explicit_keys}")

    # 1) popup_url
    if ("popup_url" in base_cols) and ("popup_url" in delta_cols):
        return ["popup_url"]
    # 2) court+area+case_no
    triple = ["auctionone_court", "auctionone_area", "case_no"]
    if all(k in base_cols for k in triple) and all(k in delta_cols for k in triple):
        return triple
    # 3) case_no
    if ("case_no" in base_cols) and ("case_no" in delta_cols):
        return ["case_no"]
    raise KeyError("공통 병합키를 찾을 수 없습니다. (popup_url | court+area+case_no | case_no)")


def _build_key_series(df: pd.DataFrame, keys: List[str]) -> pd.Series:
    if not keys:
        raise ValueError("키 목록이 비어있습니다.")
    def _mk(row) -> str:
        return "|".join(_norm_str(row.get(k, "")) for k in keys)
    return df.apply(_mk, axis=1)


def _dedup_keep_last(df: pd.DataFrame, key_col: str) -> pd.DataFrame:
    # 동일 키 중 마지막(아래쪽) 행을 유지
    return df.drop_duplicates(subset=[key_col], keep="last", ignore_index=True)


def _reorder_columns(df: pd.DataFrame) -> pd.DataFrame:
    # data_construct.py의 컬럼 순서 선호도를 반영(존재하는 컬럼만)
    preferred = [
        "auctionone_court", "auctionone_area", "case_no",
        "region_big", "region_mid", "region_small",
        "usage", "object_group", "address", "address_road", "longitude", "latitude", "coord_crs",
        "auction_date", "big_round",
        "winning_price", "second_big_price",
        "valuation_base_date", "new_build_date", "use_approval_date",
        "building_appraisal_price", "land_appraisal_price",
        "land_area", "building_area",
        "popup_url",
    ]
    cols_exist = list(df.columns)
    ordered = [c for c in preferred if c in cols_exist] + [c for c in cols_exist if c not in preferred]
    return df.loc[:, ordered]


def merge_excels(
    base_path: Optional[str],
    delta_path: str,
    output_path: str,
    base_sheet: int = 0,
    delta_sheet: int = 0,
    keys: Optional[List[str]] = None,
    prefer: str = "delta",
    drop_na_key: bool = True,
) -> Tuple[int, int, int]:
    """
    - base_path + delta_path를 키 기준으로 병합하여 output_path에 저장
    - prefer: "delta" | "base" (충돌 시 우선)
    - 반환: (base_rows, delta_rows, merged_rows)
    """
    # delta는 필수
    df_delta = _read_excel(delta_path, sheet=delta_sheet)

    # base가 없으면 delta만 복사 저장
    if not base_path or not os.path.isfile(base_path):
        out = df_delta.copy()
        out = _reorder_columns(out)
        os.makedirs(os.path.dirname(os.path.abspath(output_path)) or ".", exist_ok=True)
        out.to_excel(output_path, index=False)
        return (0, len(df_delta), len(out))

    df_base = _read_excel(base_path, sheet=base_sheet)

    # 병합키 결정
    merge_keys = _select_merge_keys(list(df_base.columns), list(df_delta.columns), explicit_keys=keys)

    # 키 시리즈 생성
    base_key = _build_key_series(df_base, merge_keys)
    delta_key = _build_key_series(df_delta, merge_keys)

    df_base = df_base.copy()
    df_delta = df_delta.copy()
    df_base["__key__"] = base_key
    df_delta["__key__"] = delta_key

    if drop_na_key:
        df_base = df_base.loc[df_base["__key__"].astype(str).str.len() > 0].copy()
        df_delta = df_delta.loc[df_delta["__key__"].astype(str).str.len() > 0].copy()

    # 내부 중복 제거(마지막 행 유지)
    df_base = _dedup_keep_last(df_base, "__key__")
    df_delta = _dedup_keep_last(df_delta, "__key__")

    # 충돌 처리: 기본은 delta 우선
    base_records = df_base.to_dict(orient="records")
    delta_records = df_delta.to_dict(orient="records")
    delta_map = {r["__key__"]: r for r in delta_records}
    base_map = {r["__key__"]: r for r in base_records}

    result: List[dict] = []
    seen: set[str] = set()

    for r in base_records:
        k = r["__key__"]
        if k in delta_map:
            chosen = delta_map[k] if prefer == "delta" else r
            result.append(chosen)
        else:
            result.append(r)
        seen.add(k)

    # base에 없던 delta만 추가
    for r in delta_records:
        k = r["__key__"]
        if k not in seen:
            result.append(r)
            seen.add(k)

    # 데이터프레임 생성 및 "__key__" 제거
    if not result:
        merged = pd.DataFrame(columns=sorted(set(df_base.columns) | set(df_delta.columns)))
    else:
        merged = pd.DataFrame(result)
    if "__key__" in merged.columns:
        merged = merged.drop(columns=["__key__"])

    # 컬럼 보정 및 저장
    merged = _reorder_columns(merged)
    os.makedirs(os.path.dirname(os.path.abspath(output_path)) or ".", exist_ok=True)
    merged.to_excel(output_path, index=False)

    return (len(df_base), len(df_delta), len(merged))


def main() -> None:
    p = argparse.ArgumentParser(description="results/auction_construct_delta.xlsx + results/auction_constructed.xlsx 병합")
    p.add_argument("--base", default="results/auction_constructed.xlsx", help="기존 전체 엑셀(없으면 delta만 저장)")
    p.add_argument("--delta", default="results/auction_construct_delta.xlsx", help="새로 생성된 델타 엑셀")
    p.add_argument("--output", default="results/auction_constructed.xlsx", help="병합 결과 출력 경로")
    p.add_argument("--base_sheet", type=int, default=0, help="base 시트 인덱스")
    p.add_argument("--delta_sheet", type=int, default=0, help="delta 시트 인덱스")
    p.add_argument(
        "--key",
        default="",
        help="콤마로 구분한 병합키(예: popup_url 또는 auctionone_court,auctionone_area,case_no). 미지정 시 자동 선택",
    )
    p.add_argument("--prefer", choices=["delta", "base"], default="delta", help="충돌 시 우선 데이터")
    p.add_argument("--no_drop_na_key", action="store_true", help="빈 키 행도 유지(기본은 제거)")
    args = p.parse_args()

    keys = [s.strip() for s in args.key.split(",") if s.strip()] if args.key else None
    base_rows, delta_rows, merged_rows = merge_excels(
        base_path=args.base,
        delta_path=args.delta,
        output_path=args.output,
        base_sheet=int(args.base_sheet),
        delta_sheet=int(args.delta_sheet),
        keys=keys,
        prefer=args.prefer,
        drop_na_key=(not args.no_drop_na_key),
    )
    print(f"[merge] base={base_rows}, delta={delta_rows} → merged={merged_rows} → {args.output}")


if __name__ == "__main__":
    main()


