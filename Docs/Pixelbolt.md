# Pixelbolt 2D 스프라이트 작업

[Pixelbolt](https://github.com/skulitom/Pixelbolt) 0.13.0, 커밋 `04668a2f2f94e081d9b5f78889c075303bf04544`을 `ThirdParty/Pixelbolt`에 고정했다. Node.js 20 이상 필요. MIT 원문 `ThirdParty/Pixelbolt/LICENSE`를 보존한다. 예제 아트도 원본 MIT 라이선스에 포함된다. Claude 프로젝트 스킬: `.claude/skills/pixelbolt/SKILL.md`.

기존 예제 `hero-sword.poses.json`의 색을 수정한 `Art/Pixelbolt/ember-guard.poses.json`은 40×36 캐릭터 시범안이다. 대기 4프레임, 걷기 8프레임, 공격 5프레임(공격 애니메이션은 같은 프레임을 반복)이다. 실제 이미지에서 기존 Tiny Swords 캐릭터와 비례·디테일이 달라 제작 실험 단계이며 게임에 아직 적용하지 않았다. 원본은 Pixelbolt의 예제이며 우리 독창적 캐릭터를 처음부터 제작했다고 주장하지 않는다.

```powershell
node ThirdParty/Pixelbolt/bin/pixelbolt.js compile Art/Pixelbolt/ember-guard.poses.json --out Art/Pixelbolt/ember-guard.json --metadata Art/Pixelbolt/ember-guard.motion.json
node ThirdParty/Pixelbolt/bin/pixelbolt.js validate Art/Pixelbolt/ember-guard.json
node ThirdParty/Pixelbolt/bin/pixelbolt.js render Art/Pixelbolt/ember-guard.json --out Tests/bin/pixelbolt-output
node ThirdParty/Pixelbolt/bin/pixelbolt.js gif Art/Pixelbolt/ember-guard.json --out Tests/bin/pixelbolt-attack.gif --animation attack --scale 4
```

산출물의 atlas JSON은 PixelFX JSON과 형식이 다르다. 기존 PixelFX Unity 메뉴에 그대로 넣지 않는다. Unity로 가져갈 때는 프레임 이름·위치·길이·반복·발 위치와 공격 프레임을 별도로 검증하고 전용 변환기나 가져오기 도구를 구현한다. 편집 레시피는 텍스트로 남겨 재생산한다. AI 이미지 생성기가 아니라 코드로 픽셀을 그리는 도구다.
