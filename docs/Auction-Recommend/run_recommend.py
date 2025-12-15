"""
은행 엑셀(Sheet C-1 new)의 모든 행에 대해
카테고리별 1순위부터 마지막 순위까지 추천을 인프로세스로 실행.

실행: python run_recommend.py
"""

import sys
import os
import time
from pathlib import Path
from typing import Dict, Any
import shutil

import yaml

from utils import (
    extract_subjects_from_bank_excel,
    category_from_usage,
    load_excel,
    build_index,
    save_results_to_csv,
)
from recommend import recommend_by_rule


PROJECT_ROOT = Path(__file__).resolve().parent
MAIN_PY = PROJECT_ROOT / "main.py"
CONFIG_PATH = PROJECT_ROOT / "cfg" / "config_recommend.yaml"
BANK_EXCEL = (
    PROJECT_ROOT
    / "data"
    / "datadisk"
    / "IBK 2025-3 Program Data Disk Final_Pool B_Mark-up-어싸인추가_테스트.xlsx"
)
BANK_SHEET = "Sheet C-1 test"
EXCEL_CANDIDATES = PROJECT_ROOT / "results" / "auction_constructed_test.xlsx"
TOPK = 100


def load_config_and_rule_counts(
    config_path: Path,
) -> tuple[Dict[str, Any], Dict[str, int]]:
    with open(config_path, "r", encoding="utf-8") as f:
        cfg: Dict[str, Any] = yaml.safe_load(f) or {}
    rules = cfg.get("rules", {}) or {}
    counts: Dict[str, int] = {}
    for cat_key, spec in rules.items():
        if isinstance(spec, list):
            counts[cat_key] = len(spec)
        elif isinstance(spec, dict):
            default_seq = spec.get("default") or []
            counts[cat_key] = len(default_seq)
    return cfg, counts


def rule_label_for(
    cfg: Dict[str, Any], category_key: str, rule_index: int, similar_land: bool
) -> str:
    rules = cfg.get("rules", {}) or {}
    cat = rules.get(category_key)
    if cat is None:
        return f"{rule_index}순위"
    if isinstance(cat, dict):
        seq = cat.get("land_like") if similar_land else cat.get("default")
    else:
        seq = cat
    try:
        idx0 = max(1, int(rule_index)) - 1
        if seq and len(seq) > idx0:
            return seq[idx0].get("name") or f"{rule_index}순위"
    except Exception:
        pass
    return f"{rule_index}순위"


def serial_from_row(row: Dict[str, Any]) -> str:
    b = str(row.get("borrower_serial_no") or "").strip()
    p = str(row.get("property_serial_no") or "").strip()
    if p.endswith(".0"):
        p = p[:-2]
    if b and p:
        return f"{b}_{p}"
    if b:
        return b
    return "row_unknown"


def main() -> None:
    if not MAIN_PY.exists():
        raise FileNotFoundError(f"main.py가 없습니다: {MAIN_PY}")
    if not CONFIG_PATH.exists():
        raise FileNotFoundError(f"설정 파일이 없습니다: {CONFIG_PATH}")
    if not BANK_EXCEL.exists():
        raise FileNotFoundError(f"은행 엑셀 파일이 없습니다: {BANK_EXCEL}")
    if not EXCEL_CANDIDATES.exists():
        raise FileNotFoundError(
            "후보 풀 엑셀(results/auction_constructed.xlsx)이 없습니다: "
            f"{EXCEL_CANDIDATES}"
        )

    # 카테고리별 마지막 순위 계산 및 설정 로드
    cfg, rule_counts = load_config_and_rule_counts(CONFIG_PATH)
    if not rule_counts:
        raise RuntimeError("config_recommend.yaml에서 rules를 찾을 수 없습니다.")

    # 은행 엑셀에서 대상 행들 적재 (main.py와 동일 함수 사용)
    df = extract_subjects_from_bank_excel(str(BANK_EXCEL), sheet_name=BANK_SHEET)
    total_rows = len(df)
    print(f"총 {total_rows}개 행에 대해 순차 실행합니다.")

    # 후보 풀 인덱스를 1회 로드
    df_index = load_excel(str(EXCEL_CANDIDATES), sheet_name=0)
    idx = build_index(df_index)
    print(f"[info] candidates loaded: {len(df_index)} rows")

    # 근린시설(건물) 비교용 후보 풀 (usage에 '근린시설' 포함)
    if "usage" in df_index.columns:
        usage_series = df_index["usage"].astype(str)
        mask_building = usage_series.str.contains("근린시설", na=False)
        df_building = df_index.loc[mask_building].copy()
    else:
        df_building = df_index.iloc[0:0].copy()
    idx_building = build_index(df_building) if not df_building.empty else None
    print(
        f"[info] building candidates(usage contains '근린시설'): {len(df_building)} rows"
    )

    for row_idx in range(total_rows):
        subj = df.iloc[row_idx].to_dict()
        usage = str(subj.get("usage") or "").strip()
        category = category_from_usage(usage)
        last_rank = rule_counts.get(category)
        if not last_rank:
            # 알 수 없는 카테고리는 최대 개수로 대체
            last_rank = max(rule_counts.values())

        serial = serial_from_row(subj)
        print(
            f"[row {row_idx:04d}] usage={usage} → category={category} → 1~{last_rank}순위 실행"
        )

        # ---- 디버그: 데이터디스크(은행 엑셀)에서 추천에 활용하는 핵심 수치 로그 ----
        def _fmt_num(v, digits=2):
            try:
                f = float(v)
                return f"{f:,.{digits}f}"
            except Exception:
                return "-"

        b_area = subj.get("area_building")  # 건물면적(평)
        l_area = subj.get("area_land")  # 토지면적(평)
        b_app = subj.get("building_appraisal_price")  # 건물감정가(원)
        t_app = subj.get("total_appraisal_price")  # 총감정가(원)
        # 평당감정가: 아파트/오피스텔/다세대는 총감정가/건물면적, 그 외는 총감정가/토지면적
        unit_by_area = subj.get("total_appraisal_price_by_area")
        if unit_by_area is None:
            try:
                u = (subj.get("usage") or "")
                denom = (
                    b_area
                    if any(x in str(u) for x in ["아파트", "오피스텔", "다세대"])
                    else l_area
                )
                if t_app is not None and denom not in (None, 0):
                    unit_by_area = float(t_app) / float(denom)
            except Exception:
                unit_by_area = None
        print(
            "  [debug] 건물면적(평)=",
            _fmt_num(b_area),
            "| 평당감정가(원/평)=",
            _fmt_num(unit_by_area, digits=0),
            "| 총감정가(원)=",
            _fmt_num(t_app, digits=0),
            "| 토지면적(평)=",
            _fmt_num(l_area),
        )

        # 유사토지는 카테고리 4(PLANT_WAREHOUSE_ETC), 5(OTHER_BIG)만 실행
        # 근린상가/사무실/아파트형공장(카테고리 3)은 제외
        run_similar = category in ("PLANT_WAREHOUSE_ETC", "OTHER_BIG")

        # 근린상가 특수 처리: 근린시설(건물)과 OTHER_BIG 규칙으로도 비교
        is_nearby_shop = "근린상가" in usage
        run_building = bool(is_nearby_shop and idx_building is not None)

        for rule_index in range(1, last_rank + 1):
            for region_scope, scope_prefix in (("big", "big"), ("mid", "mid")):
                scope_serial = f"{scope_prefix}_{serial}"

                print(f"  [scope={region_scope}] rule={rule_index}")

                # 4/5 카테고리: 유사토지 먼저 실행 → scope별 _land 폴더로 저장
                if run_similar:
                    land_label = rule_label_for(cfg, category, rule_index, similar_land=True)
                    print(f"    > in-proc similar_land rule={rule_index}, scope={region_scope}")
                    t0 = time.time()
                    try:
                        res_land = recommend_by_rule(
                            subj,
                            idx,
                            cfg,
                            rule_index=rule_index,
                            similar_land=True,
                            category_override=None,
                            rule_index_map=None,
                            topk=TOPK,
                            region_scope=region_scope,
                        )
                    except Exception as e:
                        print(
                            f"    ! 실행 실패(similar_land, rule_index={rule_index}, scope={region_scope}): {e}"
                        )
                        res_land = []
                    t1 = time.time()
                    print(f"      elapsed(similar_land, scope={region_scope}): {(t1 - t0):.3f}s")
                    land_dir = PROJECT_ROOT / "results" / f"{scope_serial}_land"
                    try:
                        land_dir.mkdir(parents=True, exist_ok=True)
                        land_out = land_dir / f"{land_label}.csv"
                        save_results_to_csv(subj, res_land, str(land_out))
                    except Exception as e:
                        print(f"      경고: 저장 실패(similar_land, scope={region_scope}): {e}")

                # 근린상가: 근린시설(건물) + OTHER_BIG 규칙으로 추가 비교
                if run_building:
                    # building_land (유사토지)
                    bld_land_label = rule_label_for(
                        cfg, "OTHER_BIG", rule_index, similar_land=True
                    )
                    print(f"    > in-proc building_land rule={rule_index}, scope={region_scope}")
                    t0 = time.time()
                    subj_building = dict(subj)
                    # usage를 근린시설로 바꿔서 usage 스코프/예외 매핑이 근린시설 기준으로 동작하도록 함
                    subj_building["usage"] = "근린시설"
                    try:
                        res_bld_land = recommend_by_rule(
                            subj_building,
                            idx_building,  # 근린시설 usage만 담은 인덱스
                            cfg,
                            rule_index=rule_index,
                            similar_land=True,
                            category_override="OTHER_BIG",
                            rule_index_map=None,
                            topk=TOPK,
                            region_scope=region_scope,
                        )
                    except Exception as e:
                        print(
                            f"    ! 실행 실패(building_land, rule_index={rule_index}, scope={region_scope}): {e}"
                        )
                        res_bld_land = []
                    t1 = time.time()
                    print(f"      elapsed(building_land, scope={region_scope}): {(t1 - t0):.3f}s")
                    bld_land_dir = PROJECT_ROOT / "results" / f"{scope_serial}_building_land"
                    bld_land_out = bld_land_dir / f"{bld_land_label}.csv"
                    try:
                        bld_land_dir.mkdir(parents=True, exist_ok=True)
                        save_results_to_csv(subj_building, res_bld_land, str(bld_land_out))
                    except Exception as e:
                        print(f"      경고: 저장 실패(building_land, scope={region_scope}): {e}")

                    # building (일반)
                    bld_label = rule_label_for(
                        cfg, "OTHER_BIG", rule_index, similar_land=False
                    )
                    print(f"    > in-proc building rule={rule_index}, scope={region_scope}")
                    t0 = time.time()
                    try:
                        res_bld = recommend_by_rule(
                            subj_building,
                            idx_building,
                            cfg,
                            rule_index=rule_index,
                            similar_land=False,
                            category_override="OTHER_BIG",
                            rule_index_map=None,
                            topk=TOPK,
                            region_scope=region_scope,
                        )
                    except Exception as e:
                        print(
                            f"    ! 실행 실패(building, rule_index={rule_index}, scope={region_scope}): {e}"
                        )
                        res_bld = []
                    t1 = time.time()
                    print(f"      elapsed(building, scope={region_scope}): {(t1 - t0):.3f}s")
                    bld_dir = PROJECT_ROOT / "results" / f"{scope_serial}_building"
                    bld_out = bld_dir / f"{bld_label}.csv"
                    try:
                        bld_dir.mkdir(parents=True, exist_ok=True)
                        save_results_to_csv(subj_building, res_bld, str(bld_out))
                    except Exception as e:
                        print(f"      경고: 저장 실패(building, scope={region_scope}): {e}")

                # 일반 실행
                label = rule_label_for(cfg, category, rule_index, similar_land=False)
                print(f"    > in-proc normal rule={rule_index}, scope={region_scope}")
                t0 = time.time()
                try:
                    res = recommend_by_rule(
                        subj,
                        idx,
                        cfg,
                        rule_index=rule_index,
                        similar_land=False,
                        category_override=None,
                        rule_index_map=None,
                        topk=TOPK,
                        region_scope=region_scope,
                    )
                except Exception as e:
                    print(
                        f"    ! 실행 실패 (rule_index={rule_index}, scope={region_scope}): {e}"
                    )
                    res = []
                t1 = time.time()
                print(f"      elapsed(normal, scope={region_scope}): {(t1 - t0):.3f}s")
                out_path = PROJECT_ROOT / "results" / scope_serial / f"{label}.csv"
                try:
                    out_path.parent.mkdir(parents=True, exist_ok=True)
                    save_results_to_csv(subj, res, str(out_path))
                except Exception as e:
                    print(
                        f"    ! 저장 실패 (rule_index={rule_index}, scope={region_scope}): {e}"
                    )


if __name__ == "__main__":
    main()



