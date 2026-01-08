# -*- coding: utf-8 -*-
"""
데이터디스크 업로드용 샘플 엑셀 파일 생성 스크립트
"""
import pandas as pd

# 샘플 데이터 - KB금융 NPL 2024-1 프로그램에 맞춤
data = [
    {
        "ProjectId": "KB-2024-1",
        "PropertyNumber": "R-001",
        "PropertyType": "아파트",
        "AddressFull": "서울특별시 강남구 역삼동 123-45 역삼아이파크 101동 1501호",
        "AddressRoad": "서울특별시 강남구 역삼로 234",
        "AddressJibun": "서울특별시 강남구 역삼동 123-45",
        "AddressDetail": "101동 1501호",
        "LandArea": 85.5,
        "BuildingArea": 112.3,
        "Floors": "15/25",
        "AppraisalValue": 1250000000,
        "MinimumBid": 1000000000,
        "Status": "pending",
        "DebtorName": "김철수",
        "CollateralNumber": "담보-001",
        "OPB": 800000000
    },
    {
        "ProjectId": "KB-2024-1",
        "PropertyNumber": "R-002",
        "PropertyType": "상가",
        "AddressFull": "서울특별시 서초구 서초동 456-78 서초타워 B1층 102호",
        "AddressRoad": "서울특별시 서초구 서초중앙로 123",
        "AddressJibun": "서울특별시 서초구 서초동 456-78",
        "AddressDetail": "B1층 102호",
        "LandArea": 45.2,
        "BuildingArea": 78.6,
        "Floors": "B1/5",
        "AppraisalValue": 850000000,
        "MinimumBid": 680000000,
        "Status": "pending",
        "DebtorName": "이영희",
        "CollateralNumber": "담보-002",
        "OPB": 500000000
    },
    {
        "ProjectId": "KB-2024-1",
        "PropertyNumber": "R-003",
        "PropertyType": "토지",
        "AddressFull": "경기도 용인시 처인구 모현읍 갈담리 산 123-1",
        "AddressRoad": "",
        "AddressJibun": "경기도 용인시 처인구 모현읍 갈담리 산 123-1",
        "AddressDetail": "",
        "LandArea": 3305.8,
        "BuildingArea": 0,
        "Floors": "",
        "AppraisalValue": 3200000000,
        "MinimumBid": 2560000000,
        "Status": "pending",
        "DebtorName": "박정민",
        "CollateralNumber": "담보-003",
        "OPB": 2000000000
    },
    {
        "ProjectId": "KB-2024-1",
        "PropertyNumber": "R-004",
        "PropertyType": "공장",
        "AddressFull": "경기도 화성시 팔탄면 노하리 567-8 삼성공장",
        "AddressRoad": "경기도 화성시 팔탄면 삼성로 456",
        "AddressJibun": "경기도 화성시 팔탄면 노하리 567-8",
        "AddressDetail": "삼성공장",
        "LandArea": 6612.5,
        "BuildingArea": 2480.3,
        "Floors": "1/2",
        "AppraisalValue": 5500000000,
        "MinimumBid": 4400000000,
        "Status": "pending",
        "DebtorName": "(주)삼성제조",
        "CollateralNumber": "담보-004",
        "OPB": 3500000000
    },
    {
        "ProjectId": "KB-2024-1",
        "PropertyNumber": "R-005",
        "PropertyType": "오피스텔",
        "AddressFull": "서울특별시 마포구 상암동 789-12 상암DMC오피스텔 2503호",
        "AddressRoad": "서울특별시 마포구 상암산로 76",
        "AddressJibun": "서울특별시 마포구 상암동 789-12",
        "AddressDetail": "2503호",
        "LandArea": 28.5,
        "BuildingArea": 42.8,
        "Floors": "25/30",
        "AppraisalValue": 450000000,
        "MinimumBid": 360000000,
        "Status": "pending",
        "DebtorName": "최수진",
        "CollateralNumber": "담보-005",
        "OPB": 320000000
    },
    {
        "ProjectId": "KB-2024-1",
        "PropertyNumber": "R-006",
        "PropertyType": "다가구주택",
        "AddressFull": "서울특별시 노원구 상계동 234-56",
        "AddressRoad": "서울특별시 노원구 상계로 123",
        "AddressJibun": "서울특별시 노원구 상계동 234-56",
        "AddressDetail": "",
        "LandArea": 198.4,
        "BuildingArea": 320.5,
        "Floors": "1-4",
        "AppraisalValue": 980000000,
        "MinimumBid": 784000000,
        "Status": "pending",
        "DebtorName": "정민호",
        "CollateralNumber": "담보-006",
        "OPB": 600000000
    },
    {
        "ProjectId": "KB-2024-1",
        "PropertyNumber": "R-007",
        "PropertyType": "아파트",
        "AddressFull": "부산광역시 해운대구 우동 1234-5 해운대자이 503동 2102호",
        "AddressRoad": "부산광역시 해운대구 해운대로 789",
        "AddressJibun": "부산광역시 해운대구 우동 1234-5",
        "AddressDetail": "503동 2102호",
        "LandArea": 72.3,
        "BuildingArea": 98.5,
        "Floors": "21/35",
        "AppraisalValue": 980000000,
        "MinimumBid": 784000000,
        "Status": "pending",
        "DebtorName": "한지수",
        "CollateralNumber": "담보-007",
        "OPB": 650000000
    },
    {
        "ProjectId": "KB-2024-1",
        "PropertyNumber": "R-008",
        "PropertyType": "빌라",
        "AddressFull": "인천광역시 남동구 구월동 567-89 구월빌라 301호",
        "AddressRoad": "인천광역시 남동구 구월남로 456",
        "AddressJibun": "인천광역시 남동구 구월동 567-89",
        "AddressDetail": "301호",
        "LandArea": 42.5,
        "BuildingArea": 68.2,
        "Floors": "3/5",
        "AppraisalValue": 320000000,
        "MinimumBid": 256000000,
        "Status": "pending",
        "DebtorName": "윤서연",
        "CollateralNumber": "담보-008",
        "OPB": 220000000
    },
    {
        "ProjectId": "KB-2024-1",
        "PropertyNumber": "R-009",
        "PropertyType": "단독주택",
        "AddressFull": "경기도 성남시 분당구 정자동 45-67",
        "AddressRoad": "경기도 성남시 분당구 정자일로 234",
        "AddressJibun": "경기도 성남시 분당구 정자동 45-67",
        "AddressDetail": "",
        "LandArea": 330.5,
        "BuildingArea": 210.8,
        "Floors": "1-2",
        "AppraisalValue": 1850000000,
        "MinimumBid": 1480000000,
        "Status": "pending",
        "DebtorName": "장우진",
        "CollateralNumber": "담보-009",
        "OPB": 1200000000
    },
    {
        "ProjectId": "KB-2024-1",
        "PropertyNumber": "R-010",
        "PropertyType": "상가",
        "AddressFull": "대전광역시 서구 둔산동 890-12 둔산타워 3층 301호",
        "AddressRoad": "대전광역시 서구 대덕대로 567",
        "AddressJibun": "대전광역시 서구 둔산동 890-12",
        "AddressDetail": "3층 301호",
        "LandArea": 56.8,
        "BuildingArea": 95.2,
        "Floors": "3/10",
        "AppraisalValue": 520000000,
        "MinimumBid": 416000000,
        "Status": "pending",
        "DebtorName": "강민재",
        "CollateralNumber": "담보-010",
        "OPB": 350000000
    }
]

# DataFrame 생성
df = pd.DataFrame(data)

# 엑셀 파일 저장
output_file = "datadisk_sample_KB_2024_1.xlsx"
df.to_excel(output_file, index=False, sheet_name="PropertyList", engine='openpyxl')

print(f"Sample Excel file created: {output_file}")
print(f"  - Total {len(df)} properties")
print(f"  - Project: KB-2024-1 (KB NPL 2024-1)")


