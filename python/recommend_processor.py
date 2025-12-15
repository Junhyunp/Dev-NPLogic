# -*- coding: utf-8 -*-
"""
NPLogic 유사물건 추천 프로세서

C#에서 호출하는 진입점
- 대상 물건 정보 JSON을 받아서
- Supabase에서 후보군(auction_cases) 조회
- 규칙 기반 추천 실행
- 결과를 JSON으로 stdout 출력

Usage:
    python recommend_processor.py <subject_json_path>
    python recommend_processor.py --subject-json '{"property_id": "...", ...}'
"""

import argparse
import json
import os
import sys
from typing import Any, Dict, List, Optional

import pandas as pd

# 환경변수 또는 설정에서 Supabase 연결 정보
SUPABASE_URL = os.environ.get("SUPABASE_URL", "")
SUPABASE_KEY = os.environ.get("SUPABASE_KEY", "")

# 로컬 모듈 import
from recommend import load_config, recommend_by_rule, recommend_all_rules
from recommend.utils import category_from_usage


def load_candidates_from_supabase(
    supabase_url: str,
    supabase_key: str,
    region_big: Optional[str] = None,
) -> pd.DataFrame:
    """
    Supabase auction_cases 테이블에서 후보군 로드.

    Args:
        supabase_url: Supabase 프로젝트 URL
        supabase_key: Supabase API 키
        region_big: 지역 필터 (시/도)

    Returns:
        후보군 DataFrame
    """
    try:
        from supabase import create_client
    except ImportError:
        # supabase 패키지가 없으면 빈 DataFrame 반환
        return pd.DataFrame()

    if not supabase_url or not supabase_key:
        return pd.DataFrame()

    client = create_client(supabase_url, supabase_key)

    query = client.table("auction_cases").select("*")

    # 지역 필터 (선택적)
    if region_big:
        query = query.eq("region_big", region_big)

    response = query.execute()

    if response.data:
        return pd.DataFrame(response.data)
    return pd.DataFrame()


def load_candidates_from_json(json_path: str) -> pd.DataFrame:
    """
    JSON 파일에서 후보군 로드 (테스트/백업용).
    """
    with open(json_path, "r", encoding="utf-8") as f:
        data = json.load(f)
    if isinstance(data, list):
        return pd.DataFrame(data)
    return pd.DataFrame()


def load_candidates_from_excel(excel_path: str, sheet_name: Any = 0) -> pd.DataFrame:
    """
    Excel 파일에서 후보군 로드 (테스트/백업용).
    """
    return pd.read_excel(excel_path, sheet_name=sheet_name)


def process_recommend(
    subject: Dict[str, Any],
    candidates_source: str = "supabase",
    candidates_path: Optional[str] = None,
    rule_index: Optional[int] = None,
    similar_land: bool = False,
    region_scope: str = "big",
    topk: int = 10,
    config_path: Optional[str] = None,
) -> Dict[str, Any]:
    """
    추천 프로세스 실행.

    Args:
        subject: 대상 물건 정보 dict
        candidates_source: "supabase", "json", "excel"
        candidates_path: JSON/Excel 파일 경로 (source가 json/excel일 때)
        rule_index: 특정 규칙만 적용 (None이면 전체)
        similar_land: 토지 유사 모드
        region_scope: 지역 범위 ("big", "mid")
        topk: 반환할 최대 건수
        config_path: 설정 파일 경로

    Returns:
        추천 결과 dict
    """
    # 1. 설정 로드
    cfg = load_config(config_path)

    # 2. 후보군 로드
    if candidates_source == "supabase":
        region_big = subject.get("region_big")
        candidates_df = load_candidates_from_supabase(
            SUPABASE_URL, SUPABASE_KEY, region_big
        )
    elif candidates_source == "json" and candidates_path:
        candidates_df = load_candidates_from_json(candidates_path)
    elif candidates_source == "excel" and candidates_path:
        candidates_df = load_candidates_from_excel(candidates_path)
    else:
        candidates_df = pd.DataFrame()

    if candidates_df.empty:
        return {
            "success": True,
            "subject": subject,
            "results": [],
            "message": "후보군 데이터가 없습니다.",
        }

    # 3. 카테고리 판별
    category = category_from_usage(subject.get("usage", ""), similar_land)

    # 4. 추천 실행
    if rule_index is not None:
        # 특정 규칙만
        results = recommend_by_rule(
            subject=subject,
            candidates_df=candidates_df,
            cfg=cfg,
            rule_index=rule_index,
            similar_land=similar_land,
            region_scope=region_scope,
            topk=topk,
        )
        recommendations = {rule_index: results}
    else:
        # 전체 규칙
        recommendations = recommend_all_rules(
            subject=subject,
            candidates_df=candidates_df,
            cfg=cfg,
            similar_land=similar_land,
            region_scope=region_scope,
            topk=topk,
        )

    # 5. 결과 정리
    total_count = sum(len(v) for v in recommendations.values())

    return {
        "success": True,
        "subject": {
            "property_id": subject.get("property_id"),
            "address": subject.get("address"),
            "usage": subject.get("usage"),
            "category": category,
        },
        "rule_results": {
            str(k): v for k, v in recommendations.items()
        },
        "total_count": total_count,
        "config": {
            "rule_index": rule_index,
            "similar_land": similar_land,
            "region_scope": region_scope,
            "topk": topk,
        },
    }


def main():
    """메인 진입점"""
    parser = argparse.ArgumentParser(
        description="NPLogic 유사물건 추천 프로세서"
    )
    parser.add_argument(
        "subject_json",
        nargs="?",
        help="대상 물건 정보 JSON 파일 경로",
    )
    parser.add_argument(
        "--subject-json",
        dest="subject_json_str",
        help="대상 물건 정보 JSON 문자열 (직접 전달)",
    )
    parser.add_argument(
        "--candidates-source",
        default="supabase",
        choices=["supabase", "json", "excel"],
        help="후보군 데이터 소스 (기본: supabase)",
    )
    parser.add_argument(
        "--candidates-path",
        help="후보군 JSON/Excel 파일 경로",
    )
    parser.add_argument(
        "--rule-index",
        type=int,
        help="특정 규칙 번호만 적용 (1-based)",
    )
    parser.add_argument(
        "--similar-land",
        action="store_true",
        help="토지 유사 모드",
    )
    parser.add_argument(
        "--region-scope",
        default="big",
        choices=["big", "mid"],
        help="지역 범위 (big=시도, mid=시군구)",
    )
    parser.add_argument(
        "--topk",
        type=int,
        default=10,
        help="반환할 최대 건수 (기본: 10)",
    )
    parser.add_argument(
        "--config",
        help="설정 파일 경로",
    )

    args = parser.parse_args()

    # 대상 물건 정보 로드
    subject = None
    if args.subject_json_str:
        try:
            subject = json.loads(args.subject_json_str)
        except json.JSONDecodeError as e:
            error = {
                "success": False,
                "error": f"JSON 파싱 오류: {e}",
            }
            print(json.dumps(error, ensure_ascii=False))
            sys.exit(1)
    elif args.subject_json:
        try:
            with open(args.subject_json, "r", encoding="utf-8") as f:
                subject = json.load(f)
        except FileNotFoundError:
            error = {
                "success": False,
                "error": f"파일을 찾을 수 없습니다: {args.subject_json}",
            }
            print(json.dumps(error, ensure_ascii=False))
            sys.exit(1)
        except json.JSONDecodeError as e:
            error = {
                "success": False,
                "error": f"JSON 파싱 오류: {e}",
            }
            print(json.dumps(error, ensure_ascii=False))
            sys.exit(1)
    else:
        error = {
            "success": False,
            "error": "대상 물건 정보가 제공되지 않았습니다.",
        }
        print(json.dumps(error, ensure_ascii=False))
        sys.exit(1)

    try:
        # 추천 실행
        result = process_recommend(
            subject=subject,
            candidates_source=args.candidates_source,
            candidates_path=args.candidates_path,
            rule_index=args.rule_index,
            similar_land=args.similar_land,
            region_scope=args.region_scope,
            topk=args.topk,
            config_path=args.config,
        )

        # JSON 출력
        print(json.dumps(result, ensure_ascii=False, indent=2, default=str))

    except Exception as e:
        error = {
            "success": False,
            "error": str(e),
        }
        print(json.dumps(error, ensure_ascii=False))
        sys.exit(1)


if __name__ == "__main__":
    main()

