# P1 · PPT 전환 전 최종 점검

2026-09-07. 사용자 요청으로 개발을 중단하고 제출 설명의 정확성과 증거 연결만 점검했다. 화면 조작·새 기능·광범위 재검증은 하지 않았다.

## 판단

Project Brain의 핵심 탐색·설계 문서·작업 및 검증 기록·MCP 연동을 설명하는 포트폴리오를 구성할 근거가 있다. 소개 범위는 **AI와 사람의 검토 근거를 연결하는 Unity Editor 도구 프로토타입**이다. 전 기능 완성, 운영 환경 검증 완료, 검토 시간 단축 실측으로 표현하지 않는다.

## 이번 읽기 점검

- 실제 Unity 프로젝트: C:/Dev/nexontutorial/ProjectBrain.
- BrainGraphService가 전체 저장 노드·관계를 읽고 계층/관계 계약 검사에 성공했다.
- Project1 / Domain5 / Feature6 / Code11 / Document11 / Evidence6, 관계68개. 이 숫자는 현재 샘플 크기이며 성능 실적이 아니다.
- Code11개: AssetDatabase GUID로 실제 파일 조회 성공, 직접 문서 연결 존재.
- Script Document 원본11개: 구조도 문서의 요약·본문과 모두 일치.
- 문서 저장 당시 코드 해시11개: 현재 해당 C# 파일과 모두 일치. 내용의 의미적 정확성·사람 승인·최신 전체 테스트를 뜻하지 않는다.
- 최신 저장 컴파일 c8bc72c0-9042-4c7a-be17-f4a09bb55da6: passed, 오류0. 코드 추가 변경 없이 기록을 확인했다.
- U1-11 Presentation15는 자체 검사 항목이다. NUnit 테스트15개라고 표현하지 않는다.

## PPT에서 말할 수 있는 범위

| 내용 | 근거와 정확한 표현 |
|---|---|
| 구조도에서 기능→코드→문서 탐색 | 등록된 계층·관계, 도메인 탐색, 코드/설계 연결 UI 구현 |
| 설계 문서와 구조도 일치 | 공통 저장·동기화 경로와 원본11개 대조. 외부 파일 전체 자동 병합은 아님 |
| 작업·오류·검증 기록 조회 | 실제 저장 기록, 고정 표시 번호, 검색·필터·검증 실행 집계 |
| AI 작업 맥락과 수정 계약 | 기존 Unity-MCP에 Brain14도구 추가. 제한된 맥락 조회·예상 버전/해시 검사·완료 조건 검사 |
| 검증 이력 | G1 EditMode31/31은 2026-09-07 12:20 KST 당시 실행 기록. 최종 프로젝트 전체 테스트 통과로 대체하지 않음 |
| 실패 검출 사례 | 과거 WorkflowDemo의 의도적 경계 오류 주입에서9/9→7/9→9/9. 현재 GAS 예제의 결함 사례로 둔갑시키지 않음 |
| 사람 확인 | 문서 저장과 사람 확인을 분리하고 미충족 완료를 거절. 인증된 승인 시스템·조직 권한 관리라고 하지 않음 |

## 중요한 한계

- 자동 C# 의존성 분석은 없다. 현재 관계는 등록된 연결이다.
- 테스트 화면은 실행 단위 집계다. 개별 케이스 상세·커버리지·모든 런타임 오류 수집은 없다.
- 현재 Brain 최종 완료 조건은 미매핑114/사람 문서 미확인11/현재 EditMode1로 미충족이다. 조건 검사·거절의 동작과 실제 완료 성공을 구분한다.
- 로컬 파일 저장과 일부 원자 쓰기/버전 검사를 사용한다. 다중 파일 전체 트랜잭션, 다중 checkout 중앙 번호 발급, 100~200명 동시 사용 검증은 없다.
- 화면 전체의 사용자 검토는 끝나지 않았다. 이전 그래프 지연의 원인도 확정되지 않았다. PPT의 정적 설명에 필요한 근거 확보와 실제 제품 전면 검증을 구분한다.
- 전체 템플릿의 새 환경 재가져오기·fresh clone 실행은 이번에 확인하지 않았다.
- 기존 도구 이름이나 프로젝트 경로를 포함한 과거 기록은 역사다. 최종 PPT에는 현재 화면과 현재 사례를 중심으로 쓴다.

## 제출 자료 판단

가장 주의할 점은 기능 누락보다 **Brain 자체의 가치와 GAS 예제가 혼동되는 것**, **본인 판단과 AI 구현 지원을 구분하지 않는 것**, **작은 글씨의 전체 화면을 그대로 넣는 것**이다. 실제 화면의 필요한 부분을 크게 쓰고 각 장에 핵심 설명 하나만 둔다.

GitHub는 비공개이며 U1-11 이후 로컬 변경을 자동 업로드하지 않았다. 외부 접근이 필요한 제출 링크는 별도 접근 확인이 필요하다. PPT만 읽어도 설명이 완결되게 구성한다.

## 근거

- 현재 구현: Packages/com.projectbrain.editor/Editor/BrainDocumentWindow.cs, BrainPresentation.cs, BrainRecordBrowser.cs, BrainDocumentSync.cs, BrainEditService.cs, BrainContextService.cs, BrainCompletionService.cs.
- 최신 컴파일: .projectbrain/evidence/c8bc72c0-9042-4c7a-be17-f4a09bb55da6.json.
- G1 테스트: .projectbrain/evidence/02252d03-cae6-4b3a-8caf-d8a337177cda.json.
- R1 통합 검사: Docs/reviews/2026-09-07-r1-fixes-evidence.json.
- 과거 경계 오류 시연: Docs/workflow-development-demo.md 및 workflow-demo-initial/regression/fixed.json.
- 최신 UI 범위: Docs/product_spec.md U1-10/U1-11, Docs/work_log.md U1-11.
