namespace EmberBastion {
 // Words for the roguelite run (r37). Pure data like StoryScript: no Unity, Tests can compile it.
 // Each boon is proposed by one of the five companions, so the camp choice is also a small story beat.
 public static class ExpeditionText {
  // Index order is Boon.* in Core: seven steady boons, seven doctrines, two camp-only boons.
  static readonly string[] names={"검술 교범","강궁","화약 개량","서리 결정","망원경","전리품 수레","전투 북","검진","속사","유폭","혹한","쇄빙","야전 대장간","불씨 방벽","불씨 살리기","군자금"};
  // Skills (r43), by troop kind. ExpeditionTextChecks holds every number here to the Core constants.
  static readonly string[] skillNames={"지면 칼바람","관통 화살","대폭발","서리 폭발","망치 신호"};
  static readonly string[] skillEffects={
   "기본 공격 4회마다 · 목표 방향 200 거리 한 줄의 적을 모두 벱니다",
   "기본 공격 5회마다 · 화살이 한 줄의 적을 모두 꿰뚫고 피해 +50%",
   "기본 공격 3회마다 · 폭발 반경 +50%, 피해 +25%",
   "기본 공격 5회마다 · 맞은 적 주변을 0.8초 얼립니다",
   "8초마다 · 지원 범위의 병력이 곧바로 한 번 더 공격합니다",
  };
  public static string SkillName(int troopKind){return skillNames[troopKind];}
  public static string SkillEffect(int troopKind){return skillEffects[troopKind];}
  // Mastery (r43): what the upgrade after the branch does, by troop kind.
  public static string MasteryEffect(int troopKind){return troopKind==4?"숙련 · 주변 공격 속도 +40% → +50%":"숙련 · 피해 +25% · "+SkillName(troopKind)+"을 기본 공격 1회 더 빨리 씁니다";}
  // Roads to the next field (r43), by Expedition.Road*. ExpeditionTextChecks holds the numbers to the Core constants.
  static readonly string[] roadNames={"평탄한 길","보급로","험로"};
  static readonly string[] roadEffects={"변화 없음","시작 골드 +100 · 온기 -6","온기 +15 · 적 체력 한 단계 더"};
  public static string RoadName(int road){return roadNames[road];}
  public static string RoadEffect(int road){return roadEffects[road];}
  // The last stand (r44).
  public const string FinaleName="최후의 방어전";
  // The goblin king (r46) comes with the last wave of the last stand.
  public const string KingName="고블린 왕";
  static readonly string[] effects={
   "검사 피해 +15%",
   "궁수 피해 +15%",
   "화약병 피해 +15%",
   "빙결 둔화 시간 +1초",
   "궁수·화약병·빙결사 사거리 +6%",
   "웨이브·대장 보상 +20%",
   "모든 병력 공격 속도 +7%",
   "교리 · 검사는 곁에 선 검사 1명마다 피해 +12% (최대 +36%)",
   "교리 · 궁수 공격 속도 +22%, 대신 궁수 사거리 -12%",
   "교리 · 화약병이 처치한 적이 터져 주변에 그 적 체력의 30% 피해",
   "교리 · 둔화된 적이 받는 추가 피해 +50% → +110%",
   "교리 · 둔화된 적이 죽으면 주변 적도 3초 둔화",
   "교리 · 대장장이가 지원하는 병력 피해 +30%",
   "교리 · 온기 손실 감소 (5바퀴당 1 → 8바퀴당 1)",
   "잔불 온기 +20",
   "다음 전장 시작 골드 +100",
  };
  static readonly Speaker[] speakers={Speaker.Rowen,Speaker.Mira,Speaker.Toto,Speaker.Serin,Speaker.Mira,Speaker.Bron,Speaker.Rowen,Speaker.Rowen,Speaker.Mira,Speaker.Toto,Speaker.Serin,Speaker.Serin,Speaker.Bron,Speaker.Bron,Speaker.Bron,Speaker.Toto};
  static readonly string[] quotes={
   "검 쥐는 법부터 다시 가르치겠습니다.",
   "시위를 더 팽팽하게 걸어 둘게요.",
   "새 배합이에요! 아마 안전할 거예요.",
   "더 오래 묶어 둘게요. 그동안 더 아프게 맞을 거예요.",
   "멀리서 먼저 보면 먼저 쏠 수 있죠.",
   "버리고 간 무기는 녹여서 다시 씁니다.",
   "북소리에 맞추면 손이 빨라집니다.",
   "어깨를 붙이고 서면 검이 두 배로 무거워집니다.",
   "멀리 쏘는 대신 빨리 쏠게요. 길 가까이에만 세워 주세요.",
   "하나가 터지면 옆에도 터져요. 원래 그런 거예요!",
   "얼어붙은 건 작은 충격에도 깨져요.",
   "얼음은 깨질 때 옆으로 번져요.",
   "제 모루 곁에 선 병사는 날이 다릅니다.",
   "단지 둘레에 벽돌을 한 겹 더 쌓겠습니다.",
   "장작을 아끼지 않겠습니다. 단지부터 살려야죠.",
   "주운 거예요. 진짜로요.",
  };
  public static string Name(int boon){return names[boon];}
  public static string Effect(int boon){return effects[boon];}
  public static Speaker Proposer(int boon){return speakers[boon];}
  public static string Quote(int boon){return quotes[boon];}
  public static int Count {get{return names.Length;}}

  static StoryLine L(Speaker s,string t){return new StoryLine(s,t);}
  // Shown the first time an expedition starts.
  public static readonly StoryLine[] Intro={
   L(Speaker.Narrator,"나루 건너에 불빛 하나가 켜진 뒤에도 겨울은 물러가지 않았다. 얼어붙은 다른 부족들이 잔불 소문을 듣고 내려오기 시작했다."),
   L(Speaker.Bron,"화덕의 불씨를 단지에 옮겨 담았습니다. 이 단지 하나로 네 전장을 돌고, 성문으로 돌아와 마지막 행렬까지 막아야 합니다. 꺼지면 처음부터 다시입니다."),
   L(Speaker.Serin,"고블린이 성채를 한 바퀴 돌 때마다 단지의 온기를 조금씩 가져가요. 오래 돌게 두지 마세요."),
   L(Speaker.Rowen,"떠나기 전에 원정대의 교리를 하나 정하시죠. 전장을 지킬 때마다 야영지에서도 하나씩 준비합니다. 무엇을 고르느냐에 따라 다음 싸움이 달라집니다."),
  };
  // One line when a later expedition starts.
  public static readonly StoryLine Again=L(Speaker.Mira,"같은 길은 두 번 없어요. 이번엔 뭐가 나올지 가 봐야 알죠.");
  // Camp greeting by the stage just cleared (0..3). The fourth camp is the one before the last stand.
  public static readonly StoryLine[] Camp={
   L(Speaker.Rowen,"성문은 지켰습니다. 숲길로 들어가기 전에 한 가지만 준비하시죠."),
   L(Speaker.Mira,"숲을 빠져나왔어요. 고개는 양쪽에서 올라오니까 준비를 잘 골라야 해요."),
   L(Speaker.Serin,"전장이 하나 남았어요. 단지가 식지 않게, 필요한 걸 골라 주세요."),
   L(Speaker.Bron,"이제 성채로 돌아갑니다. 화로 곁에서 단지를 데우겠지만, 가장 큰 행렬이 성문으로 오고 있습니다. 마지막에는 고블린 왕이 직접 옵니다."),
  };
  public static readonly StoryLine[] Won={
   L(Speaker.Narrator,"불씨 단지는 네 전장을 돌아 성문으로 돌아올 때까지 끝내 꺼지지 않았다."),
   L(Speaker.Bron,"단지가 아직 따뜻합니다. 이 불로 화덕 몇 개는 더 살릴 수 있겠어요."),
   L(Speaker.Rowen,"수고하셨습니다, 사령관님. 하지만 겨울은 길고, 다음 행렬은 또 옵니다."),
  };
  public static readonly StoryLine LostCold=L(Speaker.Bron,"…단지가 식었습니다. 화덕으로 돌아가 불씨를 다시 받아 오겠습니다.");
  public static readonly StoryLine LostCrowd=L(Speaker.Rowen,"전장을 내줬습니다. 원정대를 물리겠습니다. 다음엔 다른 준비로 가 보죠.");
 }
}
