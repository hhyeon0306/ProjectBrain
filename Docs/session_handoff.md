# Brain 세션 인계
Updated: 2026-09-06

## Current state

A1 기본 저장소·v2 이관 구현 후, 사용자 요청으로 A2 전 문서 21개를 전면 검토했다. A2는 미착수다. 실제 데이터는 Code 7개·Document 7개·Image 1개와 관계 26개이며 기존 docs 원본을 보존한다. UI는 아직 Code 중심 시제품이다.

## Decisions

최종 목업과 도메인 계층 목표를 유지한다. 문서는 현재 구현/목표/미결정을 분리했다. A2는 최소 계층·자료 탐색, 실제 증거 연결은 A3/V1이다. context는 필수 M2a, apply/update_document는 M2b로 분리했다. [검토 보고서](reviews/2026-09-06-document-review.md)에 발견과 재현 근거가 있다.

## Next action

권장 다음 단위는 A1-R Git 줄바꿈·원본 해시 재현성 보완이다. 이후 A2에서 새 모델 UI의 저장 기준·contains 규칙을 정하고 Player 탐색을 구현한다. 이번 검토는 문서 수정까지만 수행했으며 코드 보완을 끝냈다고 간주하지 않는다. 9/6 서류·PPT 시간을 확보하고 9/7 새 기능 착수 금지, 15:00 제출 목표를 우선한다.

## Verification

- A1 당시 Unity 자체 검사: 저장소 25항목·이관 18항목·기존 문서 14항목 통과. NUnit/Test Runner·플레이·UI 테스트 수가 아니다.
- 이번 문서 검토: 실제 MCP 씬 조회와 OS temp 격리 재현 성공. contains 순환·Domain 대상 verified_by 저장 허용, SaveNode 시각 자동 갱신 없음 확인.
- autocrlf=true의 임시 Git 체크아웃으로 문서 해시가 바뀌고 이관 복제본 재실행이 거절됨을 확인. 실제 제품 데이터는 수정하지 않음.
- 실데이터 읽기 노드 15개/관계 26개. 이번 리뷰에서 이전 57항목 전체 검사를 재실행하지 않음.
- 커밋 전 상위 verify -IncludeBrain 및 Markdown 22개·로컬 링크 52개·코드 펜스 확인 통과. 제품 검증과 별개다.

## Limitations

A1-R은 미해결이다. 관계별 대상 type·계층 순환 규칙, 새 UI, 실제 Evidence/Activity 상세 기록, 문서 사람 확인, 작업·완료·Brain MCP는 미구현이다. 기존 docs UI와 새 모델은 자동 동기화하지 않는다. Core 논리 분리는 단일 Editor asmdef 안에 있고 레거시 DocumentStore의 Unity 의존은 남아 있다. 사용자/Unity 기존 ProjectSettings.asset과 NuGet meta 변경 두 파일은 보존·커밋 제외한다. 관리 스크립트가 이 기존 변경으로 기록 갱신을 요구할 수 있다.
