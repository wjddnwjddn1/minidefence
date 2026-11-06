# PixelFX 사용법

PixelFX 0.3.0, commit `a84159688af3ca37a4be05a0d422b94451b2e24e`의 소스가 `ThirdParty/PixelFX`에 포함되어 있어 클라우드에서도 같은 버전을 사용한다. Node.js 18 이상이 필요하며 npm 설치는 필요 없다. Claude 프로젝트 스킬은 `.claude/skills/pixelfx/SKILL.md`다. 사용자 범위의 Claude Code 플러그인도 설치했지만 플러그인의 갱신과 프로젝트에 고정된 렌더러는 별개다.

1. `ThirdParty/PixelFX/docs/CATALOG.md`에서 이펙트 ID를 고른다.
2. 저장소 루트에서 `node Tools/PixelFX/render.mjs impacts/hit-spark`를 실행한다. `--seed`, `--fps`, `--opts` 등 원본 렌더 옵션을 전달할 수 있다. 기본 출력은 투명 배경의 액터 없는 시트이며 `--with-actor`로 액터를 포함할 수 있다. 기존 출력은 보호되며 의도적으로 교체할 때만 `--force`를 쓴다.
3. `Assets/PixelFX/Generated/<분류>/<이름>/`에 PNG와 JSON, 미리보기 contact PNG가 생긴다. Unity에서 JSON을 선택하고 **Ember Bastion > PixelFX > Import Selected JSON**을 실행하면 인접 PNG를 설정하고 프리팹을 만든다. 기존 프리팹은 덮어쓰지 않는다.
4. 생성된 프리팹을 장면에 배치하거나 코드에서 생성한다. `PixelFxPlayer`는 JSON의 프레임, 원점, 지속 시간과 반복 여부를 따른다. 텍스처 크기가 Unity 최대 텍스처 크기를 초과하면 가져오기가 실패할 수 있으므로 더 작게 생성해야 한다.

예시 `impacts/hit-spark`는 게임의 기존 타격 이벤트에도 연결되어 있다. 현재 게임 전장 렌더러는 별도 IMGUI 경로이므로 예시의 Resources 복사본을 사용한다. 다른 이펙트를 전장에 쓰려면 해당 이벤트에 연결해야 한다. Patch Pixel 글꼴과 갤러리 글꼴은 포함하거나 사용하지 않는다. 라이선스는 `Docs/ASSET-CREDITS.md`와 `ThirdParty/PixelFX/LICENSE`, `NOTICE.md`를 참조한다.
