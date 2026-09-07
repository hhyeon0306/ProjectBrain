# R1 · Project Brain 통합 설계 점검

점검일: 2026-09-07 10:51~11:05 KST. 대상: `C:/Dev/nexontutorial/ProjectBrain`, U1-8까지의 구현. 사용자가 요청한 설계·구현 검토이며 이번에 제품 코드를 수정하지 않았다. 권고 시간은 작업 예산이며 완료 보장이 아니다.

**판정: 현재 구조를 유지하고, 쓰기 경로·검증 기록 복구·맥락 선별을 보강하는 것이 맞다. 전면 재설계보다 이 세 부분의 일관성을 먼저 확보해야 한다.** 현재는 작은 Unity 프로젝트를 설명하고 작업을 보조하는 도구다. 임의의 Unity 변경을 안전하게 처리하고 실제 작업을 끝내는 완성형 도구로 설명하기에는 제약이 남아 있다.

## 유지할 설계

- 사람은 구조도·문서·작업 관리 화면, 에이전트는 ID·관계·해시·revision을 사용한다. 같은 서비스를 공유하는 분리는 적절하다.
- contains 계층과 일반 관계를 구분한다. 도메인 탐색이 외부 의존 관계를 무조건 따라가지 않는 것도 적절하다.
- 파일 저장, 설명의 최신성, 사람 확인, 실제 검증 성공을 별개로 판단한다. 미확인·오래된 검증을 완료로 인정하지 않는 방향을 유지한다.
- GUID와 노드 ID를 경로/표시 이름과 분리하고, 손상 JSON·예상 버전 불일치를 보존하며 거절한다.
- 기존 Unity-MCP를 통신·실행 기반으로 재사용한다. 별도 서버나 대규모 프레임워크 교체가 필요한 상황은 아니다.
- U1-7/8의 기본 직선·필요한 양방향 곡선·루트/도메인 크기·직접 연결 강조는 구조도 읽기에 맞다. 다음 UI 개선은 장식보다 배치 안정성이 우선이다.

## 확인한 현재 상태

| 구분 | 실제 확인 |
|---|---|
| 연결 | Ivan MCP, SampleScene 로드·미저장 변경 없음, 실제 프로젝트 경로 일치 |
| 문서 | 원본 7개와 대응 노드 7개 summary/body 일치, Code/Image의 해석 불가 GUID 0개 |
| 점검 시작 그래프 | 노드 54개, 관계 61개, 활성 작업 검증 기록 35개 |
| 기존 검증 연결 | 검증 원본은 있으나 verified_by로 연결되지 않은 기록 6개 |
| 점검 시작 활성 작업 | Movement 시연 목적, revision18(점검 요약 기록 후19), baseline240. 이후 제품 UI 개발 변경도 같은 작업에 누적됨 |
| 현재 완료 조건 | 허용 밖48·미매핑48·사람 미확인 문서2. 새 EditMode 후에도 완료 불가 |
| 실제 EditMode | 이번 실행 9/9 통과. **WorkflowDemo 대시 테스트**이며 Brain 전체의 NUnit 검증이 아님 |
| 자체 검사 | Store25, Completion32, Edit22, Lifecycle28, DocumentSync26, Map11, Scope10, DocumentStore14, Migration18, Workflow39 = **10묶음 225항목 통과** |
| 실패한 기존 검사 | BrainRepairChecks의 첫 UI assertion 실패. 현재 4탭 구조를 전제하지 않는 오래된 검사임을 별도 확인 |
| 현재 UI 오류 경계 | 임시 비표시 창에서 관계도 오류 안내·초안 보존·복구·본문 재편집 4항목 확인 |

이번 검증은 실작업의 사람 확인이나 완료 승인을 대신하지 않는다. 기존 compile `b162ac01-b548-41f9-b8c1-29d5a0719699`와 이번 EditMode `7cb3d5c0-a2bc-4697-8142-ab654d318f51`의 snapshot은 동일하다. 제품 C# 변경이 없어 컴파일을 반복하지 않았다.

## 보강 목록

### R1-01 · 검증 결과 발행 실패 후 복구 — 제출 전 우선

**문제:** 실행 결과 파일, Evidence 노드, 관계를 차례로 쓰지만 뒤쪽 저장이 실패하면 앞쪽만 남는다. 결과가 terminal 상태가 된 뒤에는 Finish 재시도가 거절되고, 실행기 Pending도 지워진다.

- 현재 데이터에서 연결 없는 검증 기록 6개 확인. 마지막 U1-8 compile도 포함한다. 목록은 evidence JSON 참조.
- 격리 시험에서 relations.json을 삭제 공유 없이 열어 Windows 파일 교체를 차단했다. Finish가 예외를 냈지만 record는 passed, 노드는 존재, 연결은 없음. 잠금을 풀어도 Finish 재시도는 거절됐다.
- 이전 Editor 로그에도 SaveRelations → Publish → Finish의 File.Replace 실패가 있다. 실제 6건 모두가 같은 원인이라고 단정하지는 않는다.
- **실제 컴파일 통과가 가짜라는 뜻은 아니다.** 실행 성공과 그래프 발행 완료가 서로 다른 상태인데 현재 복구 경로가 부족하다.

**권고:** 실행 결과는 불변 원본으로 유지하고, 노드/관계 발행을 같은 ID로 안전하게 재실행할 수 있도록 분리한다. 기존 노드는 내용이 같으면 재사용하고 다른 내용이면 거절한다. 발행 대기 기록을 남겨 재접속 시 복구하며, 임시 파일 잠금에는 제한된 재시도만 허용한다. 원본 파일 삭제 후 교체 방식으로 안전성을 낮추지 않는다.

**수용 기준:** 발행 도중 잠금/중단 → 기존 원본 보존 → 재시도 → 노드1개·연결1개, 중복 없음. 과거 6건은 실행 원본과 task 대상을 확인한 뒤 관계만 복구한다. 테스트를 다시 실행해 옛 누락을 덮는 방식은 해결이 아니다.

근거: [BrainVerification.cs:64](../../Packages/com.projectbrain.editor/Editor/BrainVerification.cs), [BrainStore.cs:154](../../Packages/com.projectbrain.editor/Editor/BrainStore.cs).

### R1-02 · 사람/에이전트 문서 쓰기 경로 충돌 — 제출 전 우선

**문제:** Script Document는 docs를 원본으로 nodes에 투영한다. 그러나 brain_update_document는 nodes의 summary/body만 변경한다. 이후 ScriptDocumentService.LoadOrCreate는 불일치를 발견하고 문서 열기를 거절한다. 저장 충돌을 막는 방어는 맞지만, 정상적인 AI 편집 다음에 사람이 이어서 편집할 길이 끊어진다.

- 격리된 허용 코드/문서에서 BrainEditService.UpdateDocument 성공 후 docs와 nodes의 실제 불일치를 재현했다.
- 실제 7문서는 현재 일치한다. 지금 자료가 이미 갈라졌다고 주장하지 않는다.
- 명세에도 단방향/역반영 미지원이 적혀 있다. 구현 실수 하나보다는 두 편집 계약의 통합이 끝나지 않은 설계 공백이다.

**권고:** 구조화된 Script Document를 편집하는 공통 서비스 하나를 사람과 에이전트가 사용하도록 한다. 역할/의도/주의/본문/첨부/연결을 명시적 필드로 전달하고 expectedVersion을 검사한다. 임의 Markdown 역파싱·자동 양방향 병합은 도입하지 않는다.

**오늘의 최소 대응:** 구조화 원본이 있는 문서를 기존 nodes 전용 MCP로 수정하는 경로를 차단/명확히 안내하고, 이미 존재하는 ScriptDocumentService의 버전 검사 저장 경로로 시연을 통일한다. 별도 일반 Document 노드의 편집 계약은 유지할 수 있다.

**수용 기준:** AI 저장 → 구조도 자동 갱신 → 사람이 같은 스크립트 문서 열기/수정 가능. 동시 초안은 조용히 덮지 않고 거절. 저장 자체는 사람 확인이 아님.

근거: [BrainEditService.cs:94](../../Packages/com.projectbrain.editor/Editor/BrainEditService.cs), [ScriptDocumentService.cs:20](../../Packages/com.projectbrain.editor/Editor/ScriptDocumentService.cs), [BrainDocumentSync.cs](../../Packages/com.projectbrain.editor/Editor/BrainDocumentSync.cs).

### R1-03 · 검증 이력이 중요한 맥락을 밀어냄 — 제출 전 우선

**문제:** BFS가 한 단계 안에서만 관계 종류를 정렬한다. Feature의 직접 검증 이력이 1단계에서 노드 예산을 먼저 차지해 2단계의 코드 설계 문서가 들어오지 않는다.

- 실제 Movement, depth2/maxNodes12/maxChars8000: 반환9노드 중 Evidence5, Document0.
- 최대 허용 maxNodes30/maxChars20000으로 늘려도 반환26노드 중 Evidence22, Document0.
- 즉 단순히 예산을 늘리는 것으로 해결되지 않는다. 문서 ID를 알고 직접 조회하면 읽을 수 있으나, 처음 맥락을 찾는 에이전트에 불리하다.

**권고:** 기본 조회는 기능→구현 코드→설계 문서를 우선 확보하고, Evidence/Activity는 최신 요약 또는 종류별 소수만 제공한다. 전체 이력은 명시적으로 요청한다. 다음 조회 ID도 관련 자료 우선으로 제공한다. 양방향 관계·출처·생략 수·분량 상한은 유지한다.

**수용 기준:** 이력 0/10/100개 상황에서 같은 Feature의 핵심 코드·문서가 기본 예산에 포함되고, 이력 생략과 후속 조회가 정확히 표시됨. 성능/토큰 절감 수치는 별도 비교 전까지 주장하지 않음.

근거: [BrainContextService.cs:46](../../Packages/com.projectbrain.editor/Editor/BrainContextService.cs).

### R1-04 · 검사 코드와 현재 UI 계약 불일치 — 제출 전 검증 정리

BrainRepairChecks는 첫 화면의 공통 message에 관계 오류가 나타나고 본문과 관계도가 함께 있다고 가정한다. 현재는 관계도 탭 안에 HelpBox와 재시도 버튼이 만들어진다. 원본 검사는 실제 실패했다. 별도 현재 구조용 시험은 관계 오류 안내·초안 보존·복구·본문 재편집 모두 통과했다.

**권고:** 기존 UI 회귀 검사를 실제 탭 진입과 인라인 오류 확인 방식으로 갱신한다. Brain 자체 검사를 Test Runner에 연결하거나 적어도 실행 진입점을 하나로 정리한다. 당장 전부 NUnit으로 옮길 필요는 없지만, 오래된 61항목 통과 기록을 현재 결과로 제시하면 안 된다. EditMode9/9는 별도 대시 예제다.

근거: [BrainRepairChecks.cs:76](../../Packages/com.projectbrain.editor/Editor/BrainRepairChecks.cs), [BrainDocumentWindow.cs:164](../../Packages/com.projectbrain.editor/Editor/BrainDocumentWindow.cs), [현재 구조용 재현 코드](2026-09-07-r1-ui-probe.cs.txt).

### R1-05 · 숨긴 기록이 그래프 배치를 바꿈 — 시간이 남으면 첫 UI 보강

BrainMapView는 숨김 적용 전에 모든 노드로 배치한다. 구조도는 문서 저장/새로 읽기에서 새 MapView를 만들며 수동 위치·줌·이동 상태를 넘기지 않는다. 따라서 검증 기록 증가나 문서 저장 뒤 사용자가 외운 위치가 바뀐다.

격리한 현재 의미 그래프에 Evidence 하나를 추가하고 숨겼을 때 일반 노드 **19개 모두** 좌표가 달라졌다. 화면 좌표 저장 여부를 포함한 동작 개선이 필요하다.

**권고:** 기본 배치는 의미 노드로 계산하고 기록은 별도 주변 배치한다. 새로 읽기 시 기존 ID의 좌표·줌·이동을 유지하고 새 노드만 배치한다. 화면 맞춤/재배치는 사용자가 원할 때 수행한다. 대규모 레이아웃 엔진 교체는 뒤로 미룬다.

근거: [BrainMapView.cs:219](../../Packages/com.projectbrain.editor/Editor/BrainMapView.cs), [BrainExplorerWindow.cs:42](../../Packages/com.projectbrain.editor/Editor/BrainExplorerWindow.cs).

### R1-06 · 완료 정책이 실제 Unity 변경 범위를 다 담지 못함 — 시연 경계 확정 필요

Assets/Packages/ProjectSettings와 meta까지 감시하지만, 변경 매핑은 Code 노드의 자산 경로/lastKnownPath만 찾는다. 격리 시험에서 허용한 `Assets/A.cs.meta`도 unmapped-change였다. 씬·설정·이미지 등의 정상 작업을 완료시키는 명시적 정책이 부족하다.

현재 Movement 활성 작업에 Brain UI 개발까지 누적되어 허용 밖48/미매핑48이 남는다. 허용 경로를 넓혀도 미매핑 문제가 없어지는 것은 아니다. 단순 경고가 아니라 현재 제품의 지원 범위다.

**오늘:** 범위 밖 변경을 숨기거나 기준선을 초기화하지 않는다. 사람 확인/완료 성공을 보여줄 대상과 기존 누적 작업 처리 방식을 명시적으로 결정한다. 결정이 없으면 거절 시연과 격리 정책 검사까지만 설명한다.

**이후:** 코드 부속 meta·씬·프리팹·프로젝트 설정 등 변경 유형별 소유 영역과 검토 정책을 모델링한다. 미등록 파일을 일괄 승인하는 해법은 부적절하다.

근거: [BrainCompletionService.cs:113](../../Packages/com.projectbrain.editor/Editor/BrainCompletionService.cs), [BrainWorkspace.cs](../../Packages/com.projectbrain.editor/Editor/BrainWorkspace.cs).

### R1-07 · '관련 코드'와 '실제 의존' 의미 혼용 — 설명 정확성 보강

문서의 relatedScriptGuids를 depends_on으로 투영한다. 예를 들어 DemoHudPresenter는 DemoGameFlow 타입을 사용하지 않지만 등록된 관계에는 반대 방향 연결이 존재한다. 관련성 자체는 유효해도 실제 코드 의존이라고 말하면 부정확하다.

**오늘:** 수동 등록 관계라는 표시를 눈에 잘 보이게 하고 시연에서도 자동 코드 분석이라고 설명하지 않는다. **이후:** related_to 같은 일반 관련성과 실제 depends_on을 구분하고 출처를 보존한 이관을 설계한다. 실제 의존을 확인하지 않고 한쪽 선을 임의 삭제하지 않는다.

근거: [BrainDocumentSync.cs:95](../../Packages/com.projectbrain.editor/Editor/BrainDocumentSync.cs), [DemoHudPresenter.cs](../../Assets/Scripts/UI/DemoHudPresenter.cs), [DemoGameFlow.cs](../../Assets/Scripts/Core/DemoGameFlow.cs).

### R1-08 · 현재 명세/인계/설명 자료 정리 — 제출 전에 짧게

사용법·architecture·task에 초기 단계의 미구현/다음 작업 설명이 누적돼 있다. 일부는 이후 절이 대체한다고 명시했지만, 독자가 순서대로 읽으면 현재 계약을 재구성해야 한다. 상위 작업표는 U1-3, 인계의 Verification은 U1-4/5 결과가 먼저 나오는 식의 시간 차도 있다.

**권고:** 맨 앞에 현재 계약·도구13개·지원/미지원 범위·현재 검증표를 하나로 두고, 단계별 과거 설명은 archive로 연결한다. P1에는 문제/직접 구현/재사용/실제 증거/한계를 한 장으로 정리한다. 복잡한 UI보다 이 설명이 제출물 신뢰도에 더 직접적이다.

## 이후로 미룰 설계

- 변경 영향 범위에 맞는 최신성/문서 확인 해시. 지금은 무관한 의미 관계 변경도 모든 문서 확인 해시에 영향을 줌을 격리 시험에서 확인했다. 보수적인 거절은 안전하지만 작업이 많아지면 재확인 비용이 커진다.
- 메인 스레드의 반복 전체 파일 해시/그래프 재읽기 축소. 현재 status 호출은 여러 서비스에서 Capture를 반복한다. 캐시/증분 색인과 최종 확인 시 재스캔의 책임을 분리하되, 대규모 성능을 측정하기 전 수치를 주장하지 않는다.
- 새 프로젝트의 도메인/기능 등록·에이전트 검색 진입점. 현재 시연 데이터와 ID를 알고 시작하는 방식에서 일반 사용자 프로젝트로 확장할 때 필요하다.
- 그래프 선택 해제/키보드 이동, 일반 필터 상태의 재컴파일 보존, 긴 이름 툴팁, 역할 요약과 Code summary의 소유 정책 정리.
- 이력 보존 기간/용량 제한, 손상 파일 격리 조회, schema migration과 내보내기 정책. 단일 작성자 전제의 한계를 다중 프로세스 트랜잭션 보장으로 확대 해석하지 않는다.
- 자동 코드 의존 분석, PlayMode/Player 빌드 정책, 즐겨찾기, 대규모 UI 전면 재디자인.

## 복귀 후 4시간 권고 배분

1. **0~60분:** R1-01/02/03 중 시연을 막는 최소 보강과 R1-04 검사 정리. 60분을 넘길 상황이면 지원 범위를 명시하고 개발을 끊는다. 세 항목의 전체 구현을 60분에 약속하는 일정이 아니다.
2. **60~90분:** 실제 탐색 → 작업 재개/맥락 → 변경 감지/거절 → 검증을 짧게 시연하고 증거를 확보한다. 사람 확인·완료 성공은 실제 조건을 충족한 경우만 포함한다.
3. **90~210분:** 서류·기존 포폴 PPT와 Brain 설명 정리. 원본 문항·글자 수·PPT·영상 링크·첨부 요구는 이 작업 공간에서 아직 확인하지 못했다.
4. **210~240분:** 최종 답변/첨부/링크 확인과 제출 준비. 내부 목표 15:00, 공식16:00은 구분한다. 복귀 시각이 늦어지면 개발 시간을 먼저 줄인다.

## 근거와 제한

- [격리 재현 및 데이터 감사 결과](2026-09-07-r1-evidence.json)
- [재현 코드](2026-09-07-r1-probes.cs.txt): 실제 데이터 읽기 + 별도 Temp 시험 저장소. 잠금/가상 통과 값은 제품 검증 증거가 아닌 재현용 fixture다.
- [추가 검사·실제 EditMode 결과](2026-09-07-r1-validation.json)
- 제품 C#/USS, 실제 문서·코드·사람 확인·기준선·허용 범위는 수정하지 않았다. 실제 EditMode 실행 기록과 작업 진행 요약만 제품 기록에 추가했다.
- 기존 사용자 변경5경로를 보존한다. 기존 씬242행 공백으로 전체 Git 관리 검사가 실패하는 것은 제품 테스트 실패와 별개다.
- 이번 전체 점검에서 새 마우스/모든 해상도 QA, PlayMode/Player 빌드, 새 clone import, 외부 사용자의 실제 장기 사용은 검증하지 않았다.
