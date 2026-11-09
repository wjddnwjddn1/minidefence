# 기존 그림 출처와 사용 범위

- r20 UI 교체 이후 `WoodTable`, `UIRibbons`, `UIBarBase`, `UIBarFill`, `UIBigBarBase`, `UIBigBarFill`, `UIBigRed`, `UIBigRedPressed`, `UIPaper`, `UISelection`, `UISpecialPaper`는 현재 화면 코드에서 불러오지 않는다. 아래 기존 출처와 해시는 이력으로 유지한다. 파일은 Resources에 남아 있으므로 빌드에 포함될 수 있다. 게임 지형과 캐릭터, 원형 버튼과 아이콘은 계속 사용한다.

- Pixel Frog / Tiny Swords: https://pixelfrog-assets.itch.io/tiny-swords
- 실제 기준: `C:/퍼블리싱/htdocs/static/ai/ember-bastion/play/ASSET-CREDITS.md`의 구버전 CC0 자료 설명.
- 사용한 실제 파일은 사용자가 지정한 `C:/ai로 만든 프로그램/tiny assets/Tiny Swords/Tiny Swords (Update 010)`에서 골랐다. 기존 검사·궁수, 고블린·폭약병, 병사, 화살, 폭발, 평지·고지대·물·거품·그림자·나무·성채 등을 로컬 비교판에 사용한다. 선택 경로와 SHA-256은 `tiny-selected-assets.json`에서 확인할 수 있다. 기존 검사·궁수 파일도 해당 원본과 SHA가 일치한다.
- 새 Free Pack의 커스텀 라이선스와 구버전 CC0를 혼합하지 않는다. 원본 ZIP, raw, SVG, 개인 캡처, 전체 원본팩은 포함·배포하지 않았다. 이 프로젝트는 아직 공개하거나 원격 저장소에 올리지 않았다.
- 폭탄병은 기존 병사 유휴 동작과 투사체를 사용한다. 폭탄 전용 투척 모션이나 캐릭터 60종 완성으로 보고하지 않는다.
- 나눔고딕은 [Google Fonts의 Nanum Gothic](https://github.com/google/fonts/tree/main/ofl/nanumgothic), SIL Open Font License 1.1. 원문은 `NanumGothic-OFL.txt`.
- 바위 고개 장식은 웹판의 `play/assets/map-decor-runtime.png`(SHA-256 `413589ba8b047f38d2fbe1b0773c0bf6a367d8609cba3de4a9b7ca4bf256f1ac`)에서 rock1/rock2만 표시한다. 이는 사용자가 지정한 Pixel Frog Tiny Swords Free Pack의 선택된 게임용 런타임 크롭이며, 웹판 `ASSET-CREDITS.md`와 `docs/ember-bastion-map-decor-provenance.json`의 Free Pack 커스텀 프로젝트 사용 조건을 따른다. 구버전 CC0와 구분하며 원본 팩은 포함하거나 공개하지 않는다.
- 사용자가 제공한 Tiny Swords Free Pack의 `tilemap_color3.png`를 `ForestGround.png`(SHA-256 `558588391E6DD91C444E6A0CE4B2F8EE0EE13E50BAEB8289837A42E58B2C8F1A`), `tilemap_color4.png`를 `PassGround.png`(SHA-256 `944F8FE48D87F60234A7EAC9648E444BBECDABAABD0B478758EA45C882603503`)로 사용한다. 기존 UI에는 `WoodTable.png`(SHA-256 `D2ABDB31E7FDC3CC2BCAD7988C03AAAB28AA73A2CB5F869B615C157595034A79`)를 사용했다. 이 파일에는 [Free Pack의 별도 이용 조건](https://pixelfrog-assets.itch.io/tiny-swords)을 적용하며 구버전 CC0와 구분한다. 원본 에셋을 재배포하거나 판매하지 않는다.
- 같은 Free Pack의 `Terrain/Tileset/Tilemap_color5.png`를 4번째 전장 "습지 나루"의 바닥 `MarshGround.png`(SHA-256 `327AC5A34D6210FBEBE71DB19B1BE91E41950EA2AD370C4E19BB243D57E3C80B`)로 사용한다(r26, Claude). 가져오기 설정은 `ForestGround.png`와 같다. 장식은 기존 `Tree`, `ForestTree`, `BlueHouse`, `map-decor-runtime`의 덤불·바위를 다시 쓴다.
- 숲길 장식은 같은 Free Pack의 `Terrain/Resources/Meat/Sheep/Sheep_Idle.png`를 `SheepIdle.png`로 사용한다(SHA-256 `BA3B743BDD5D851C0EBF195BC4E168729601D5598C0F4D9A1E39D5E0C778E14E`). 장식은 전투 계산에 참여하지 않는다.
- 같은 Free Pack의 `UI Elements/UI Elements/Ribbons/SmallRibbons.png`를 `UIRibbons.png`(SHA-256 `DA0EB7C5F1B15CA42C3A53906854C9DE9C885166E16DC11A0A2832C6DEA9A930`), `Bars/SmallBar_Base.png`를 `UIBarBase.png`(`D5191F8A82B98E7049E379AE26F8BE84CB56D1077D6ECFE36DE4DF0EB5E7EEB8`), `Bars/SmallBar_Fill.png`를 `UIBarFill.png`(`06CB099925AAF847B63C068B3E002CD9B0DEB324BC11AFBFB80258A7D0818F3D`)로 사용했다. `Particle FX/Explosion_01.png`는 `FxExplosion.png`(`2753EA93DAF727590DF7C8A0E7C5759ADA6ED185B63DEE037DE317ACE9530509`)로 사용한다. 해당 Free Pack의 이용 조건을 따르며 구버전 CC0와 구분한다.
- Free Pack `Units/Blue Units`의 `Warrior/Warrior_Idle.png` → `WarriorIdle.png`(`E612B408908C938999025FA05C45D81B23A31D8B68A6934072ADFCCF1D5D716C`), `Warrior/Warrior_Attack1.png` → `WarriorAttack.png`(`DA150DB2442454BD661BD8E2512DAB2C7DEF4D9202334A06DD9D6A8EDFB20FEC`), `Archer/Archer_Idle.png` → `ArcherIdle.png`(`A82EAA45223C26C4C0C70F131E2CE9252F0FF774E5CEAA3C64E2E3DEF32E31A3`), `Archer/Archer_Shoot.png` → `ArcherShoot.png`(`FCAF0FB2992BAA1709DE88069444000EA2F7967517A42E0E7AFD84DA52926249`)를 사용한다.
- Free Pack `Buildings/Blue Buildings/House1.png`를 `BlueHouse.png`(`F68FBCE236CA663E2C1D86484F4617586BC99F4F9B634C493E62FA7EEECE5C63`), `UI Elements/UI Elements/Icons/Icon_05.png`를 `IconSword.png`(`60386728A8A367F6660C25443B3DF64149762DC882FC8DAE881A49CDE0933866`)로 사용한다.
- Free Pack `Particle FX/Fire_01.png`는 화상 상태 표시 `FxFire.png`(`0856990D6216260DF7F6C9B422B9128BD242E35BAAEBB1AE2F2955FC336696B5`)로 사용한다. 화상 시간과 피해는 기존 전투 로직을 따른다.
- Free Pack `Terrain/Resources/Wood/Trees/Tree3.png`를 숲길 장식 `ForestTree.png`(`E2317A54BB801E52F4C2FBFD4D9D91611036D7B001FE623A92B55539FEDDAAE2`)로 사용한다.
- 구버전 `Tiny Swords (Update 010)/Factions/Goblins/Troops/TNT/Dynamite/Dynamite.png`는 폭탄 투사체 표시 `Dynamite.png`(`5CFC3D6FA12655CD05A2562065993EB5C7CE85C685589490EAB92FAEE189A5B5`)로 사용한다. 이 파일은 위에 적힌 구버전 CC0 출처로 분리해 기록한다.

- Free Pack `Units/Blue Units/Monk/Idle.png`를 `FrostIdle.png`(SHA-256 `A72CC63BE3151C3AF61E8F8C86129B8A0C563C8731E04AE89A27E616BB6CB962`), `Monk/Heal.png`를 `FrostCast.png`(`6620F483DCDE826628E1754B57445B2FA4C36C077BAC3FA1C6B3D828E55FBDE5`)로 사용한다. 서리 병과 전용 그림이 아닌 수도사 동작을 임시로 사용하며 치유 동작의 초록빛 효과가 보일 수 있다. Free Pack의 별도 이용 조건을 적용한다.

- Free Pack `Units/Blue Units/Pawn/Pawn_Idle Hammer.png`를 기술자의 대기 그림 `SmithIdle.png`로 사용한다 (SHA-256 `DA78BE28921999740C3BEAD474F764587F64AB488B3173D52FC81B37B5E6376A`). 원본은 망치 든 일꾼이며 기술자 전용 제작물은 아니다. Free Pack 별도 이용 조건을 따른다.

- 구버전 `Tiny Swords (Update 010)/Factions/Goblins/Troops/Torch`의 Yellow, Blue, Purple 색상 시트를 각각 `EnemyRunner.png` (`5D57FA095FEB61270CF84BFBD5D3FB3BC6493BE9BEF7DDA4A7EA233803D33768`), `EnemyShield.png` (`8BE105DFB696767A447402258EEB1D8A95EE89B444FCE83A003E7B654933F448`), `EnemyBoss.png` (`9F19A5A789AD5DF29B37EF60E6C3111B0C4118E89515E337FFB44052E6A2691A`)로 사용한다. 기존 구버전 CC0 출처이며, 색상 변형을 적 역할 구분에 사용하는 임시 표현이다. 별도 적 원화는 아니다.

- Free Pack `UI Elements/UI Elements/Icons/Icon_06.png`를 방패병의 갑옷 표시 `ArmorIcon.png` (`E8CAABED224049145D642FFAE2D11924B5C06DFCD4E12196881904EBE8655FD5`)로 사용한다. Free Pack 별도 이용 조건을 따른다.

- Free Pack 런타임 아틀라스 `map-decor-runtime.png`의 duck 3프레임을 물가 장식으로 사용한다. 게임 상태에 영향을 주지 않으며 위에 기재한 Free Pack 별도 이용 조건과 출처를 따른다.

- Free Pack `Units/Blue Units/Monk/Heal_Effect.png`를 성채 수리 효과 `FxRepair.png` (`2635DD7445D573E878B578ECCE858484B4D2889268819EC35657CF416F8AAF33`)로 사용한다. 원본은 수도사의 치유 이펙트이며 Free Pack 별도 이용 조건을 따른다.

- Free Pack `Particle FX/Water Splash.png`를 서리 적중 임시 효과 `FxFrostImpact.png` (`06743825027D551231A6DE5AA700708EAB5DC61DBD785314C80785AD595D7680`)로 사용한다. 원본은 물 튀김 이펙트이며 Free Pack 별도 이용 조건을 따른다.

- Free Pack `Buildings/Blue Buildings/Tower.png`를 전장의 장식 `BlueTower.png`로 사용한다 (SHA-256 `AC0F01128D951C9DA4CFE811AF175A35868B6D91CDDA04FABBE9DCF909CAF868`). 장식은 전투에 영향을 주지 않으며 Free Pack의 별도 이용 조건을 따른다.
- Free Pack `Units/Red Units/Warrior/Warrior_Run.png`을 방패병의 이동 표현 `EnemyShieldRun.png`으로 사용한다 (SHA-256 `74B9D13A4B57F75A048CB42AF35EF5C0A20791F67A9B03F9E93C72790DBE2948`). 원본은 붉은 기사이며 기존 고블린 색 변형 대신 방패를 시각적으로 나타낸다. 방어 수치는 바꾸지 않으며 Free Pack 별도 이용 조건을 따른다.

- Free Pack `UI Elements/UI Elements/Buttons/SmallBlueSquareButton_Regular.png`과 `SmallBlueSquareButton_Pressed.png`를 각각 `UIPlateBlue.png` (SHA-256 `D0C41E7710BC76EA927F462D483EEA3F51AC8288BB532513040347818A7E6F86`)와 `UIPlateBluePressed.png` (`8493BAD77ADAD5EAA0C4CFBB9D986E9E174E27563D563CEAA0D410BA7E4105FE`)로 이전 UI에서 사용했다. 게임 내 표시 시 투명 여백을 제외하며 Free Pack 별도 이용 조건을 따른다.

- Free Pack `Buttons/BigRedButton_Regular.png` → `UIBigRed.png`, SHA-256 `F68A753C9CB974B9B8A4AF83804A7952A8EF1532EF5441449102660119209B81`.
- Free Pack `Buttons/BigRedButton_Pressed.png` → `UIBigRedPressed.png`, SHA-256 `33B6D42455AA10D66EFC02FBB855CE96A558263601B3BA3C74CE6C61844A857D`.
- Free Pack `Papers/RegularPaper.png` → `UIPaper.png`, SHA-256 `4A91BBDE509AF741580E514961ED25DD309695536046536B600AB63BEC080A1E`.
- Free Pack `Cursors/Cursor_04.png` → `UISelection.png`, SHA-256 `A87D50DA08983B801743C07151F3F5B1A1D7A76440F02223B8CA3E4DAA03CBBA`.
- Free Pack `Icons/Icon_03.png` → `UICoin.png`, SHA-256 `87E913C8CAC52427DADBA7C1688DA6B105EAF63437F8D1BCCC3C9F33EDD0658B`.

- Free Pack `Buttons/TinyRoundBlueButton.png` → `UIBadgeRound.png`, SHA-256 `C2522C3017925FE6A7D1E0AA3988FB7F3E1A6991430572D872904C2964E89186`.

- Free Pack `Buttons/SmallRedRoundButton_Regular.png` → `UIEmberRound.png`, SHA-256 `D5B53A9A685E3029924D1BF842D240506F82911B742F7258706304C81D5074F2`.

- Free Pack `Buttons/SmallRedRoundButton_Pressed.png` → `UIEmberRoundPressed.png`, SHA-256 `7A63DC9E5718F60E0ED338CFE771AB98BF250A280D063F0D60EA27A1B092BA78`.

- Free Pack `Buttons/TinyRoundRedButton.png` → `UIBadgeRed.png`, SHA-256 `A9029EB0045D75742677ED7F4316D37B3A5359CCDD17D0E124CD98ED9517B7E9`.

- Free Pack `Icons/Icon_05.png` → `UIBadgeSword.png`, SHA-256 `60386728A8A367F6660C25443B3DF64149762DC882FC8DAE881A49CDE0933866`.

- Free Pack `Icons/Icon_06.png` → `UIBadgeShield.png`, SHA-256 `E8CAABED224049145D642FFAE2D11924B5C06DFCD4E12196881904EBE8655FD5`.

- Free Pack `Icons/Icon_10.png` → `UIBadgeGear.png`, SHA-256 `299E8F79FC2C82455277DDDD2C6E709EB68723370414D3488E3227347F5F6CEA`.

- Free Pack `UI Elements/UI Elements/Papers/SpecialPaper.png`를 기존 보조 버튼 배경 `UISpecialPaper.png`로 사용했다. SHA-256 `26C0AD30C7858E3B0F09454E74BC63104D43AC25328A3DBA48B42779096C023D`. Free Pack 이용 조건을 적용한다.

- Free Pack `Bars/BigBar_Base.png`와 `BigBar_Fill.png`를 기존 적 수 바로 사용했다. SHA-256은 각각 `A1053C7B5176B80DF5240D504A5D948337C3FE0BA6F4AF0BB0DDA0776071C0AC`, `1EEE58410178874F54D2A66AB77B7C66EA1670823720F9CBF115E761AF8C8CF2`. Free Pack 이용 조건을 적용한다.


## PixelFX

- [PixelFX](https://github.com/patch-gg/pixelfx), version 0.3.0, commit `a84159688af3ca37a4be05a0d422b94451b2e24e`: Copyright (c) 2026 Patch and Hugo Duprez, MIT License. Full license: `ThirdParty/PixelFX/LICENSE`.
- Portions adapt Sprite Fusion VFX, Copyright (c) 2026 Hugo Duprez, and Hollow Crown, Copyright (c) 2026 rehan-remade, both MIT; see `ThirdParty/PixelFX/NOTICE.md`.
- Patch Pixel and other gallery fonts are not included or used. Generated `impacts/hit-spark` sprite sheet derives from PixelFX.
- `Tools/PixelFX/fx/impacts/eb-hit.js` (project effect, rendered to `Assets/Resources/PixelFX/HitSteel`, `HitArrow`, `HitFrost`) is original project code that uses the PixelFX core and follows the inked-wedge approach of PixelFX `impacts/blunt-hit` (MIT).
- `Tools/PixelFX/fx/status/eb-burn.js` (project effect, rendered to `Assets/Resources/PixelFX/Burn`) draws its flame tongues with PixelFX `status/_lib.js`, which adapts Sprite Fusion's open-source fire (MIT, Copyright (c) 2026 Hugo Duprez).
- `Tools/PixelFX/fx/status/eb-freeze.js` and `eb-guard.js` (candidates, rendered to `Art/PixelFX/candidates/Freeze`, `Guard`; not used by the game yet) are original project code drawn with the PixelFX core (MIT). They copy no PixelFX effect shapes.


## Pixelbolt

- [Pixelbolt](https://github.com/skulitom/Pixelbolt), version 0.13.0, commit `04668a2f2f94e081d9b5f78889c075303bf04544`; Copyright (c) 2026 PixelForge contributors, MIT License. Full license: `ThirdParty/Pixelbolt/LICENSE`.
- `Art/Pixelbolt/ember-guard.poses.json` and generated prototype derive from Pixelbolt `examples/hero-sword.poses.json` with palette changes. Prototype is not currently used in the game.
