# Riftrail 프로젝트 정책

- 목적: Riftrail의 프로젝트 범위와 Agent Core 소비 계약을 소유한다.
- 읽는 시점: Core 정책을 읽은 뒤 프로젝트 작업을 시작할 때.
- 책임: 사용자가 범위와 승인을 정하고 프로젝트 에이전트가 구현·검증한다.
- 상태: 활성 정책. 초기 기반 구성.
- 관련 권위: [Core 정책](core/PROJECT_RULES.md), 현재 사용자 요청.

## 프로젝트 범위

Riftrail은 독립 Private GitHub 프로젝트다. 저장소는 Private으로 유지한다.
현재 범위는 Agent Core 연결, 프로젝트 진입과 스킬 등록이다. 제품 기능·프레임워크·도메인 구조는 사용자 요청으로 정한다.
Core는 부모 gitlink로 고정한 submodule이며 host 역할에서 읽기 전용으로 소비한다.
프로젝트 산출물은 이 Consumer root에 보존하고 Core 안에는 생성하지 않는다.
`private/`와 `.env`는 보호 경로이며 별도 대상·목적 승인 없이 열거하거나 읽지 않는다.

## 소비 계약

<!-- agent-core-consumer:v1 -->
```json
{
  "contract_version": 2,
  "consumer_role": "host",
  "core_path": "core",
  "state": "SESSION_HANDOFF.md",
  "entry_pointers": {
    "codex": "AGENTS.md",
    "claude": "CLAUDE.md"
  },
  "required_core_capabilities": {},
  "rule_roots": [],
  "protected_paths": ["private", ".env"]
}
```
<!-- /agent-core-consumer:v1 -->

## 프로젝트 스킬 운용

Codex와 Claude Code 양쪽에 선택한 Core의 공개 설치 목록에서 `scope=project`인 스킬을 등록한다. 명시적 스킬 제외는 없다.
`common`, `internal`, 미분류 항목은 프로젝트 등록에서 제외하며 개인용 공통 설치는 별도 요청으로 수행한다.
향후 승인된 Core 갱신에는 두 도구의 신규 공개 프로젝트 스킬 등록과 기존 진입 대조를 포함한다. 같은 등록 범위의 파일마다 재승인을 요구하지 않는다.
동명 공급자 충돌·수동 수정·제거된 공개 항목은 자동 덮어쓰기나 삭제 없이 보고한다.
호출 정책은 배포 메타데이터를 유지하며 native 진입은 공통 WORKFLOW로 연결한다.

## 도메인 규칙 라우팅

<!-- core-rule-routes:v1 -->
| 행동 | 읽을 소유자 |
|---|---|
<!-- /core-rule-routes:v1 -->
