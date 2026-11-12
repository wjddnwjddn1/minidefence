using UnityEngine;

namespace EmberBastion {
 public sealed partial class BattleView {
  int pendingCampBoon=-1;
  int[] campDoctrineCounts;
  readonly System.Collections.Generic.Dictionary<int,(int waves,string enemies)> routeSummaries=new System.Collections.Generic.Dictionary<int,(int,string)>();

  void CaptureCampComposition(Battle completed){
   var counts=new int[Boon.BattleCount];
   foreach(var unit in completed.Units){
    if(unit.Kind==0&&completed.Units.Exists(other=>other!=unit&&other.Kind==0&&Point.Distance(completed.Field.Slots[unit.Slot],completed.Field.Slots[other.Slot])<=Boon.PhalanxReach))counts[Boon.Phalanx]++;
    if(unit.Kind==1)counts[Boon.RapidFire]++;
    if(unit.Kind==2)counts[Boon.ChainBlast]++;
    if(unit.Kind==3){counts[Boon.DeepFrost]++;counts[Boon.Shatter]++;}
    completed.SupportBonuses(unit,out double haste,out _);
    if(haste>0)counts[Boon.FieldForge]++;
   }
   campDoctrineCounts=counts;
  }

  string DoctrineComposition(int boon){
   if(boon==Boon.EmberWard)return "원정 전체에 적용";
   if(campDoctrineCounts==null||campDoctrineCounts.Length!=Boon.BattleCount)return "직전 편성 기록 없음";
   string kind=boon==Boon.Phalanx?"인접 검사":boon==Boon.RapidFire?"궁수":boon==Boon.ChainBlast?"화약병":boon==Boon.FieldForge?"지원 받는 병력":"빙결사";
   return "직전 편성 · "+kind+" "+campDoctrineCounts[boon]+"명";
  }

  void ChooseCampPreparation(Expedition run,int boon){
   int[] fields=run.NextFields();
   if(fields.Length==1)CommitCampChoice(run,boon,fields[0]);
   else if(fields.Length>1)pendingCampBoon=boon;
  }

  void CommitCampChoice(Expedition run,int boon,int field){
   if(!run.Choose(boon,field))return;
   pendingCampBoon=-1;
   campDoctrineCounts=null;
   sound.Play("upgrade");
   OpenExpeditionBattle();
  }

  (int waves,string enemies) RouteSummary(int field){
   if(routeSummaries.TryGetValue(field,out var summary))return summary;
   var preview=Battle.CreateForRules(1,field,4);
   var totals=new int[Balance.Enemies.Length];
   for(int wave=0;wave<preview.WaveCount;wave++)
    foreach(var group in preview.WaveGroups(wave))totals[group.Kind]+=preview.SpawnCount(group);
   summary=(preview.WaveCount,"질주 "+totals[1]+" · 방패 "+totals[2]+" · 폭약 "+totals[3]+" · 대장 "+totals[4]);
   routeSummaries.Add(field,summary);
   return summary;
  }

  void DrawCampRoutes(Expedition run,Rect area,bool wide){
   int[] fields=run.NextFields();
   int selectedField=-1;
   float height=(area.height-35-(fields.Length-1)*5)/fields.Length;
   for(int i=0;i<fields.Length;i++){
    int field=fields[i];
    Rect card=new Rect(area.x,area.y+i*(height+5),area.width,height);
    ExpeditionRect("route"+field,card);
    if(MenuButton(card,""))selectedField=field;
    float mapWidth=wide?100:72;
    FieldMap(new Rect(card.x+7,card.y+5,mapWidth,card.height-10),Balance.Fields[field]);
    float x=card.x+mapWidth+16,width=card.xMax-x-8;
    hud.wordWrap=false;
    hud.alignment=TextAnchor.MiddleLeft;
    hud.fontSize=wide?15:12;
    hud.normal.textColor=uiGold;
    int threat=Mathf.RoundToInt((float)(Boon.ThreatHealth*(run.Stage+1)*100));
    var summary=RouteSummary(field);
    string title=Balance.Fields[field].Name+" · "+summary.waves+"웨이브 · 적 체력 +"+threat+"%";
    while(hud.fontSize>10&&MeasureText(hud,title).x>width)hud.fontSize--;
    GUI.Label(new Rect(x,card.y+3,width,card.height/2),title,hud);
    hud.fontSize=wide?13:11;
    hud.normal.textColor=uiText;
    GUI.Label(new Rect(x,card.center.y,width,card.height/2-2),summary.enemies,hud);
   }
   Rect back=new Rect(area.x,area.yMax-30,area.width,30);
   ExpeditionRect("routeBack",back);
   if(MenuButton(back,"준비 다시 고르기"))pendingCampBoon=-1;
   if(selectedField>=0)CommitCampChoice(run,pendingCampBoon,selectedField);
  }
 }
}
