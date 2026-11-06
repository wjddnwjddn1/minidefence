# PixelFX 사용법

PixelFX 0.3.0, commit `a84159688af3ca37a4be05a0d422b94451b2e24e`의 소스가 `ThirdParty/PixelFX`에 포함되어 있어 클라우드에서도 같은 버전을 사용한다. Node.js 18 이상이 필요하며 npm 설치는 필요 없다. Claude 프로젝트 스킬은 `.claude/skills/pixelfx/SKILL.md`다. 사용자 범위의 Claude Code 플러그인도 설치했지만 플러그인의 갱신과 프로젝트에 고정된 렌더러는 별개다.

1. `ThirdParty/PixelFX/docs/CATALOG.md`에서 이펙트 ID를 고른다.
2. 저장소 루트에서 `node Tools/PixelFX/render.mjs impacts/hit-spark`를 실행한다. `--seed`, `--fps`, `--opts` 등 원본 렌더 옵션을 전달할 수 있다. 기본 출력은 투명 배경의 액터 없는 시트이며 `--with-actor`로 액터를 포함할 수 있다. 기존 출력은 보호되며 의도적으로 교체할 때만 `--force`를 쓴다.
3. `Assets/PixelFX/Generated/<분류>/<이름>/`에 PNG와 JSON, 미리보기 contact PNG가 생긴다. Unity에서 JSON을 선택하고 **Ember Bastion > PixelFX > Import Selected JSON**을 실행하면 인접 PNG를 설정하고 프리팹을 만든다. 기존 프리팹은 덮어쓰지 않는다.
4. 생성된 프리팹을 장면에 배치하거나 코드에서 생성한다. `PixelFxPlayer`는 JSON의 프레임, 원점, 지속 시간과 반복 여부를 따른다. 텍스처 크기가 Unity 최대 텍스처 크기를 초과하면 가져오기가 실패할 수 있으므로 더 작게 생성해야 한다.

예시 `impacts/hit-spark`의 Resources 복사본(`HitSpark`)은 파이프라인 검사용으로 남아 있지만 전장에서는 더 이상 그리지 않는다. 빛 위주의 이펙트는 밝은 모래·풀 위에서 거의 보이지 않았다.

## 게임 전용 이펙트 (`Tools/PixelFX/fx`)

원본 라이브러리(`ThirdParty/PixelFX`)는 고치지 않는다. 게임에 맞춘 이펙트는 `Tools/PixelFX/fx/<분류>/<이름>.js`에 같은 규약(`meta`, `draw(p, t, o)`)으로 두고, `Tools/PixelFX/game-effects.json`에 시트 이름·이펙트 ID·옵션(팔레트, 시드)을 적는다. `node Tools/PixelFX/render-game.mjs`가 시트를 `Assets/Resources/PixelFX/<이름>.png/.json`에, 미리보기를 `Docs/pixelfx-game-effects/`에 만든다. 원본의 `tools/check.mjs`는 원본 폴더만 보므로, 게임 전용 이펙트는 `node Tests/PixelFxGameEffects.mjs`로 확인한다. 시트가 소스와 같은지, 한 번 재생하는 이펙트는 마지막 프레임이 비는지, 반복 이펙트는 빈 프레임이 없는지를 본다.

- `impacts/eb-hit`: 잉크 테두리가 있는 흰 별, 튀어나갔다 돌아오는 쐐기 6개, 잉크 테두리 파편 4개. 0.45초, 72×52, 20fps. 원본 `impacts/blunt-hit`의 잉크 쐐기 방식을 따른다(MIT).
- 시트 3종: `HitSteel`(검사, 강철색), `HitArrow`(궁수, 주황), `HitFrost`(서리, 하늘색). `Battle`의 `hit` 신호에 실린 `Source`(0 검사, 1 궁수, 3 서리)로 고른다. 폭약(2)과 잔불은 자체 폭발 이펙트가 있어 타격 이펙트를 그리지 않는다. 전장에서는 별의 중심(시트 좌표 36,30)을 맞은 적의 몸 중심에 두고 2배(144×104)로 그린다. 파편은 시트 바닥(y 44)에서 튀므로 적의 발 근처에 떨어진다.
- `status/eb-burn` → `Burn`: 화상 상태 반복 이펙트. 잉크 테두리 불꽃 혀 3개가 흔들리고, 잉크 테두리 불씨가 올라간다. 0.6초, 12프레임, 48×56, 20fps. 원본 `status/_lib.js`의 불꽃 혀(Sprite Fusion fire 각색, MIT)에 잉크 층을 하나 더했다. 전장에서는 적의 상체에 1.4배로 겹쳐 그리고, 적마다 위상을 달리한다(`e.Id`). 예전 `FxFire`는 불꽃→연기→빈 칸을 반복해 화상이 깜빡이듯 보였다. 지금은 시트가 없을 때만 대신 쓴다.

Patch Pixel 글꼴과 갤러리 글꼴은 포함하거나 사용하지 않는다. 라이선스는 `Docs/ASSET-CREDITS.md`와 `ThirdParty/PixelFX/LICENSE`, `NOTICE.md`를 참조한다.
