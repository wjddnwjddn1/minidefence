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

## 후보 이펙트 (게임 미적용)

게임이 아직 읽지 않는 후보는 `Tools/PixelFX/candidate-effects.json`에 적고 `node Tools/PixelFX/render-candidates.mjs`로 `Art/PixelFX/candidates/<이름>.png/.json`과 미리보기 `-contact.png`를 만든다. `Assets/Resources`에 넣지 않으므로 게임 빌드와 `Tests/PixelFxGameEffects.mjs`(적용된 4종만 검사)에 영향이 없다. `BattleView`에 연결하고 승인되면 `game-effects.json`과 그 검사의 기대 목록으로 옮긴다.

| 이름 | 효과 ID | 대상 | 규격 | 상태 (11-09) |
| --- | --- | --- | --- | --- |
| `Freeze` | `status/eb-freeze` | 빙결 `Enemy.FreezeTime>0` (지금은 몸에 하늘색 색조만) | 56×36, 기준점 28,30(발), 0.6초 12프레임 20fps 반복 | 후보. 게임 미연결, 사람 미승인 |
| `Guard` | `status/eb-guard` | 대장 함성 보호 `Enemy.Shield>0` (지금은 호박색 점선 원) | 72×42, 기준점 36,30(발), 0.8초 16프레임 20fps 반복 | 후보. 게임 미연결, 사람 미승인 |

- `eb-freeze`: 발목을 둘러싼 잉크 테두리 얼음 결정 7개와 발밑 서리 자국이다. 결정이 조금씩 숨 쉬듯 자라고, 흰 반짝임이 결정 모서리를 타고 오른다. 원본 `status/frozen`은 기본 캐릭터 마스크를 감싸는 방식이라 우리 적에는 맞지 않아 새로 그렸다.
- `eb-guard`: 발밑에서 도는 호박색 점선 고리(8조각, 한 바퀴에 한 조각씩 돌아 이음매 없이 반복)와, 몸 바깥 양옆에서 엇박으로 오르내리는 잉크 테두리 방패 표식 2개다.
- 둘 다 머리·손·무기 위에는 아무것도 그리지 않는다(발과 몸 바깥만 사용). 상태 반복 효과라 공격처럼 준비→발동→소멸→복귀를 따르지 않는다. 대신 프레임마다 불투명 픽셀이 빙결 475~497개, 보호 563~585개로 빈 프레임이 없고, 같은 소스는 같은 결과를 내며, 캔버스 가장자리에 닿지 않는다.
- 연결할 때 권장: 적 크기 112 기준으로 효과 1px을 1.25 월드 단위로 그리고 기준점을 적의 발(스프라이트 아랫단)에 둔다. 빙결은 1초, 보호는 3초만 유지되므로 켜지고 꺼질 때 0.1초 정도 투명도를 주면 갑자기 생기고 사라지는 느낌이 줄어든다(미검증 제안).

Patch Pixel 글꼴과 갤러리 글꼴은 포함하거나 사용하지 않는다. 라이선스는 `Docs/ASSET-CREDITS.md`와 `ThirdParty/PixelFX/LICENSE`, `NOTICE.md`를 참조한다.
