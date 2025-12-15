# -*- coding: utf-8 -*-
from __future__ import annotations

"""
main.py

규칙 기반 추천 실행 진입점.
- 단건(--subject_case) 또는 JSON 배치(--json_glob)를 받아
  config의 rules에 정의된 순위 규칙을 적용해 Top-K를 산출/저장합니다.
  
- python main.py --excel results\auction_constructed.xlsx --bank_excel "data\datadisk\IBK 2025-3 Program Data Disk Final_Pool B_Mark-up-어싸인추가_테스트.xlsx" --bank_sheet "Sheet C-1 test" --bank_row_index 38 --category_override OTHER_BIG --all_ranks --topk 30 --override_usage "근린시설"
"""

import argparse
import os

from recommend import load_config, recommend_by_rule, run_batch_from_json
from utils import load_excel, build_index, extract_subjects_from_bank_excel, save_topk_to_excel, save_results_to_csv, parse_rule_map, category_from_usage


    


def run_recommend(args):
    """단건 실행: 사건번호로 대상 행을 찾아 규칙 기반 추천 수행."""
    cfg = load_config(args.config)
    df = load_excel(args.excel, sheet_name=args.sheet)
    idx = build_index(df)

    subj_df = df.loc[df["case_no"] == args.subject_case]
    if subj_df.empty:
        raise ValueError(f"대상 사건번호를 찾을 수 없음: {args.subject_case}")
    subj = subj_df.iloc[0].to_dict()
    # 실행 시 usage 덮어쓰기(선택)
    if args.override_usage:
        subj["usage"] = str(args.override_usage)

    results = recommend_by_rule(
        subj,
        idx,
        cfg,
        rule_index=args.rule_index,
        similar_land=args.similar_land,
        category_override=args.category_override if args.category_override else None,
        rule_index_map=parse_rule_map(args.rule_map),
        topk=args.topk,
        region_scope=args.region_scope,
    )

    print(f"\n[대상] {subj.get('case_no')} | {subj.get('usage')} | {subj.get('address')}")
    for i, r in enumerate(results, 1):
        comp = r["candidate"]
        print(f"{i}) 사건번호: {r['case_no']} | 규칙: {r.get('detail',{}).get('rule')} | {comp.get('usage')} | {comp.get('address')}")
    if args.output:
        project_root = os.path.dirname(os.path.abspath(__file__))
        out_path = args.output
        if not os.path.isabs(out_path):
            out_path = os.path.join(project_root, out_path)
        save_topk_to_excel(subj, results, out_path)


    


def main():
    """CLI 파서 구성 및 실행 모드 분기."""
    parser = argparse.ArgumentParser(description="경매 유사사례 추천 - 규칙 기반")
    parser.add_argument("--excel", required=True, help="엑셀 파일 경로 (전체 데이터)")
    parser.add_argument("--sheet", type=int, default=0, help="시트 인덱스(기본 0)")
    parser.add_argument("--subject_case", help="대상 사건번호(case_no)")
    parser.add_argument("--json_glob", default="", help="대신 JSON들을 일괄 실행(세미콜론으로 여러 패턴)")
    parser.add_argument("--bank_excel", default="", help="은행 엑셀에서 직접 주제행을 읽어 추천 수행")
    parser.add_argument("--bank_sheet", default="Sheet C-1 new", help="은행 엑셀 시트명")
    parser.add_argument("--bank_row_index", type=int, default=0, help="은행 엑셀에서 단일 행 인덱스(0-기반)")
    parser.add_argument("--config", default="cfg/config_recommend.yaml", help="설정 파일 경로(YAML/JSON)")
    parser.add_argument("--rule_index", type=int, default=1, help="규칙 순위 인덱스(1부터)")
    parser.add_argument("--similar_land", action="store_true", help="유사토지 규칙 사용")
    parser.add_argument("--category_override", default="", help="카테고리 강제 (빈값이면 자동)")
    parser.add_argument("--all_ranks", action="store_true", help="해당 카테고리의 모든 순위를 순차 실행")
    parser.add_argument("--override_usage", default="", help="주제 행의 Property Type(usage)을 이 값으로 덮어쓰기")
    parser.add_argument("--rule_map", default="", help="카테고리별 rule 인덱스 지정. 예: APT_OFFICETEL=1,ROWHOUSE_MULTI=2")
    parser.add_argument("--region_scope", choices=["big", "mid"], default="big", help="지역 필터 스코프: 'big'=대지역(시/도), 'mid'=(대+중)지역(시/군/구)")
    parser.add_argument("--topk", type=int, default=30, help="추천 상위 개수")
    parser.add_argument("--output", default="", help="단건 결과 저장 경로(선택)")
    args = parser.parse_args()

    if args.bank_excel:
        # 은행 엑셀에서 단일 행만 주제로 추천 수행
        df_all = extract_subjects_from_bank_excel(args.bank_excel, sheet_name=args.bank_sheet)
        if args.bank_row_index < 0 or args.bank_row_index >= len(df_all):
            raise IndexError(f"--bank_row_index가 범위를 벗어났습니다. (0 <= idx < {len(df_all)})")
        subj = df_all.iloc[int(args.bank_row_index)].to_dict()
        # 실행 시 usage 덮어쓰기(선택)
        if args.override_usage:
            subj["usage"] = str(args.override_usage)

        # 전체 후보 풀(엑셀 인덱스) 로드
        cfg = load_config(args.config)
        df_index = load_excel(args.excel, sheet_name=args.sheet)
        idx = build_index(df_index)

        project_root = os.path.dirname(os.path.abspath(__file__))
        # 폴더명: 차주일련번호 + '_' + Property일련번호 (둘 다 없으면 row_XXXX)
        b = str(subj.get("borrower_serial_no") or "").strip()
        p = str(subj.get("property_serial_no") or "").strip()
        if p.endswith(".0"):
            p = p[:-2]
        if b and p:
            serial = f"{b}_{p}"
        elif b:
            serial = b
        else:
            serial = f"row_{int(args.bank_row_index):04d}"

        # 카테고리/규칙 시퀀스
        _rule_map = parse_rule_map(args.rule_map)
        _cat = args.category_override if args.category_override else category_from_usage(subj.get("usage"))
        _rules = cfg.get("rules", {})
        _cat_cfg = _rules.get(_cat)
        if isinstance(_cat_cfg, dict):
            _seq = _cat_cfg.get("land_like") if args.similar_land else _cat_cfg.get("default")
        else:
            _seq = _cat_cfg
        last_rank = len(_seq) if _seq else 0

        def _rule_name(idx1: int) -> str:
            if _seq and len(_seq) >= idx1:
                try:
                    return _seq[max(1, idx1) - 1].get("name") or f"{idx1}순위"
                except Exception:
                    return f"{idx1}순위"
            return f"{idx1}순위"

        if args.all_ranks and last_rank > 0:
            for ri in range(1, last_rank + 1):
                try:
                    results = recommend_by_rule(
                        subj,
                        idx,
                        cfg,
                        rule_index=ri,
                        similar_land=args.similar_land,
                        category_override=args.category_override if args.category_override else None,
                        rule_index_map=None,
                        topk=args.topk,
                        region_scope=args.region_scope,
                    )
                except Exception as e:
                    print(f"! 실행 실패 (rule_index={ri}): {e}")
                    results = []
                label = _rule_name(ri)
                out_path = os.path.join(project_root, "results", serial, f"{label}.csv")
                save_results_to_csv(subj, results, out_path)
                print(f"Saved: {out_path} (rows={len(results)})")
        else:
            _applied_idx = int(_rule_map.get(_cat, args.rule_index) or args.rule_index)
            results = recommend_by_rule(
                subj,
                idx,
                cfg,
                rule_index=args.rule_index,
                similar_land=args.similar_land,
                category_override=args.category_override if args.category_override else None,
                rule_index_map=_rule_map,
                topk=args.topk,
                region_scope=args.region_scope,
            )
            label = _rule_name(_applied_idx)
            out_path = os.path.join(project_root, "results", serial, f"{label}.csv")
            save_results_to_csv(subj, results, out_path)
            print(f"Saved: {out_path} (rows={len(results)})")
        return

    if args.json_glob:
        run_batch_from_json(args, save_topk_to_excel)
        return
    if not args.subject_case:
        parser.error("--subject_case 또는 --json_glob 중 하나를 지정하세요.")
    # 단건 실행 전, usage 덮어쓰기를 적용하기 위해 run_recommend 내부에서 처리
    run_recommend(args)


if __name__ == "__main__":
    main()
    