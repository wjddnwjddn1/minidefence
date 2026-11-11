namespace EmberBastion {
 // Story script (user request 2026-11-11: opening, interludes, why the enemy attacks, character dialogue, monster lines).
 // Pure data, no Unity: Tests can compile it. Story bible: Docs/스토리-대본-20261111.md.
 public enum Speaker {Narrator,Rowen,Mira,Toto,Serin,Bron,Grnak,Goblin}
 public sealed class StoryLine {public readonly Speaker Who;public readonly string Text;public StoryLine(Speaker who,string text){Who=who;Text=text;}}
 public static class StoryScript {
  public static string Name(Speaker s){switch(s){case Speaker.Rowen:return "검사 로웬";case Speaker.Mira:return "궁수 미라";case Speaker.Toto:return "화약병 토토";case Speaker.Serin:return "빙결사 세린";case Speaker.Bron:return "대장장이 브론";case Speaker.Grnak:return "고블린 대장 그르낙";case Speaker.Goblin:return "고블린";default:return "";}}
  // Troop kind for ally portraits (0 sword .. 4 smith), enemy kind for goblins (+10), -1 none.
  public static int Portrait(Speaker s){switch(s){case Speaker.Rowen:return 0;case Speaker.Mira:return 1;case Speaker.Toto:return 2;case Speaker.Serin:return 3;case Speaker.Bron:return 4;case Speaker.Grnak:return 14;case Speaker.Goblin:return 10;default:return -1;}}
  static StoryLine L(Speaker s,string t){return new StoryLine(s,t);}

  public static readonly StoryLine FrostIntro=L(Speaker.Serin,"제가 늦춘 적은 다른 분들 공격에 더 아프게 맞아요.");

  // Shown once, before the very first preparation.
  public static readonly StoryLine[] Opening={
   L(Speaker.Narrator,"겨울이 끝나지 않는 골짜기. 마지막 남은 불씨가 이 성채의 화덕에서 타고 있다. 사람들은 그것을 '잔불'이라 불렀다."),
   L(Speaker.Bron,"사령관님, 화덕의 잔불이 꺼지면 골짜기 전체가 얼어붙습니다. 다른 건 몰라도 이 불만은 지켜야 해요."),
   L(Speaker.Rowen,"북쪽 숲에서 횃불 행렬이 내려옵니다. 고블린들입니다. 노리는 건 잔불이고요."),
   L(Speaker.Mira,"횃불까지 들고 와서 불을 훔치러 온다니, 웃기지도 않네요."),
   L(Speaker.Serin,"…저 횃불들, 이미 꺼져 가고 있어요. 저는 북쪽에서 왔어요. 고블린의 땅은 지난겨울에 통째로 얼어붙었어요."),
   L(Speaker.Rowen,"사정이야 어떻든 성채는 못 내줍니다. 사령관님, 병력을 세울 자리를 정해 주십시오."),
  };

  // Shown once per field, at its first preparation (field 0 follows the opening).
  public static readonly StoryLine[][] FieldIntro={
   new[]{
    L(Speaker.Toto,"성문 앞이라면 제 화약이 제일 잘 터지죠! 아, 성문은 안 터뜨립니다. 아마도."),
    L(Speaker.Rowen,"적은 정해진 길로만 옵니다. 길이 많이 닿는 초록 자리부터 채우십시오."),
   },
   new[]{
    L(Speaker.Mira,"숲길은 굽이가 많아요. 굽이 안쪽에 서면 같은 적을 두 번 쏠 수 있죠."),
    L(Speaker.Serin,"질주병이 많아요. 제가 발을 묶어 두면 다른 분들이 맞히기 쉬워질 거예요."),
    L(Speaker.Grnak,"인간들! 불을 내놔라! 내 부족은 봄까지 기다릴 수 없다!"),
   },
   new[]{
    L(Speaker.Bron,"고개는 양쪽에서 올라옵니다. 성채 바로 앞자리를 비워 두면 안 됩니다."),
    L(Speaker.Rowen,"방패병이 섞여 있습니다. 폭발은 방패에 반만 들어가니 궁수와 함께 쓰십시오."),
    L(Speaker.Serin,"저 깃발… 북쪽 얼음 마을에서 봤던 거예요. 그르낙은 마을을 통째로 이끌고 내려온 거예요."),
   },
   new[]{
    L(Speaker.Serin,"나루를 건너면 고블린의 땅이에요. 여기서 막히면 그들은 돌아갈 곳이 없어요."),
    L(Speaker.Grnak,"이번엔 물러서지 않는다. 잔불이 없으면 우리 새끼들이 얼어 죽는다!"),
    L(Speaker.Mira,"…저 말, 거짓말 같지는 않네요."),
    L(Speaker.Rowen,"그래도 지금은 막아야 합니다. 이야기는 이긴 다음에 하죠."),
   },
  };

  // Shown once, after the first marsh victory.
  public static readonly StoryLine[] Ending={
   L(Speaker.Grnak,"…불씨 하나면 됐는데. 하나면, 우리 마을 화덕도 다시 탔을 텐데."),
   L(Speaker.Serin,"사령관님. 저들이 원한 건 성채가 아니었어요. 따뜻한 불 하나였어요."),
   L(Speaker.Bron,"잔불은 나눠 붙여도 줄지 않습니다. 불씨 단지 하나쯤은 나루 건너로 보낼 수 있어요."),
   L(Speaker.Rowen,"…좋습니다. 대신 다음에 올 때는 횃불 말고 빈손으로 오라고 전하십시오."),
   L(Speaker.Narrator,"그해 겨울, 나루 건너편에도 작은 불빛 하나가 켜졌다. 하지만 골짜기 너머에는 아직 꺼지지 않은 겨울이 남아 있었다."),
  };

  // Short non-blocking lines during combat.
  public static readonly StoryLine[] FirstWave={
   L(Speaker.Rowen,"온다! 자리를 지켜라!"),
   L(Speaker.Mira,"숲 냄새가 나요. 질주병이 먼저 올 거예요."),
   L(Speaker.Bron,"양쪽 다 봐야 합니다. 한쪽만 막으면 뚫려요."),
   L(Speaker.Serin,"물 위에서는 느려져요. 그때를 노리세요."),
  };
  public static readonly StoryLine[] MidWave={
   L(Speaker.Toto,"펑! 아, 이 소리 좋다."),
   L(Speaker.Mira,"절반 왔어요. 화살은 아직 넉넉해요."),
   L(Speaker.Bron,"화덕은 아직 따뜻합니다. 버티세요."),
   L(Speaker.Serin,"그르낙의 북소리가 가까워져요."),
  };
  public static readonly StoryLine LastWave=L(Speaker.Rowen,"마지막 행렬입니다! 이것만 막으면 됩니다!");
  public static readonly StoryLine[] BossArrive={
   L(Speaker.Grnak,"길을 비켜라, 작은 인간들!"),
   L(Speaker.Grnak,"잔불을 가진 자가 겨울을 이긴다!"),
   L(Speaker.Grnak,"그르낙은 두 번 지지 않는다!"),
   L(Speaker.Grnak,"마지막이다! 불을 가져간다!"),
  };
  public static readonly StoryLine BossRoar=L(Speaker.Grnak,"형제들아, 방패를 들어라!");
  public static readonly StoryLine BossRoarReply=L(Speaker.Serin,"주변에 방패를 세웠어요! 대장부터 떼어 놓으세요!");
  public static readonly StoryLine CastleHit=L(Speaker.Bron,"화덕이 흔들립니다! 성채가 맞았어요!");
  public static readonly StoryLine Crowded=L(Speaker.Mira,"너무 많아요! 빈 자리 남았나요?");
  public static readonly StoryLine[] Victory={
   L(Speaker.Rowen,"첫 행렬을 막았습니다. 하지만 이건 정찰대일 뿐입니다."),
   L(Speaker.Serin,"숲이 조용해졌어요. 그르낙은 고개 쪽으로 돌아갔대요."),
   L(Speaker.Bron,"고개를 지켰습니다. 잔불이 아직 따뜻합니다."),
   L(Speaker.Mira,"나루가 조용해요. 이번엔… 정말 끝난 걸까요?"),
  };
  public static readonly StoryLine[] Defeat={
   L(Speaker.Rowen,"물러납니다! 자리를 다시 잡고 오죠."),
   L(Speaker.Mira,"숲에서 길을 잃었어요. 굽이 안쪽을 다시 봐요."),
   L(Speaker.Bron,"고개를 내줬습니다. 양쪽을 같이 막아야 해요."),
   L(Speaker.Grnak,"불은 우리 것이다! 다음에도 온다!"),
  };

  // Goblin barks by enemy kind (0 torch, 1 runner, 2 shield, 3 bomber).
  public static readonly string[][] Barks={
   new[]{"불! 불을 가져와!","따뜻한 쪽으로!","추워… 추워…","저 성에 불이 있다!"},
   new[]{"빨리빨리!","먼저 가면 불 쬔다!","헥헥…","안 잡히지롱!"},
   new[]{"막아라, 막아!","방패 뒤로!","화살 따위!","천천히, 천천히…"},
   new[]{"펑! 펑!","다 날려 버려!","심지 어디 갔지?","불… 불 좀 빌려줘!"},
  };
 }
}
