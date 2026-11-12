using System;
using UnityEngine;
namespace EmberBastion {
 // r43 (Claude): ember grades on screen. Winning a run at a grade opens the next one (rules: Expedition.Grade, Boon.GradeHealth).
 // The player picks the grade for the next run on the result panel or on the empty expedition panel.
 public sealed partial class BattleView {
  const string ExpeditionGradeKey="snomet.ember-bastion.unity-comparison.expedition.grade.v1",ExpeditionGradePickKey="snomet.ember-bastion.unity-comparison.expedition.grade-pick.v1";
  // Kept in memory as well, so a session that may not store preferences still opens grades as it wins.
  int expeditionGradeOpenSession,expeditionGradePick=-1;

  // The highest grade the player may start: one above the highest grade won.
  int ExpeditionGradeOpen {get{int stored=0;if(PreferencesAllowed())try{stored=PlayerPrefs.GetInt(ExpeditionGradeKey,0);}catch(Exception){}return Mathf.Clamp(Mathf.Max(stored,expeditionGradeOpenSession),0,Expedition.MaxGrade);}}
  // The grade the next run starts at. Defaults to the highest open grade.
  int ExpeditionGrade {get{
   int open=ExpeditionGradeOpen;
   if(expeditionGradePick<0){expeditionGradePick=open;if(PreferencesAllowed())try{expeditionGradePick=PlayerPrefs.GetInt(ExpeditionGradePickKey,open);}catch(Exception){}}
   return Mathf.Clamp(expeditionGradePick,0,open);
  }}
  void SetExpeditionGrade(int grade){
   expeditionGradePick=Mathf.Clamp(grade,0,ExpeditionGradeOpen);
   if(PreferencesAllowed())try{PlayerPrefs.SetInt(ExpeditionGradePickKey,expeditionGradePick);PlayerPrefs.Save();}catch(Exception){}
   ExpeditionGradeLog();
  }
  // Called when a run ends. A win at the highest open grade opens the next and selects it.
  bool UnlockExpeditionGrade(Expedition run){
   if(run==null||run.State!=Expedition.Status.Won)return false;
   int open=Mathf.Min(Expedition.MaxGrade,run.Grade+1);if(open<=ExpeditionGradeOpen)return false;
   expeditionGradeOpenSession=open;
   if(PreferencesAllowed())try{PlayerPrefs.SetInt(ExpeditionGradeKey,open);PlayerPrefs.Save();}catch(Exception){}
   SetExpeditionGrade(open);return true;
  }
  void ExpeditionGradeLog(){if(Debug.isDebugBuild&&Application.platform==RuntimePlatform.WebGLPlayer)Debug.Log("EXPEDITION_GRADE|"+ExpeditionGradeOpen+"|"+ExpeditionGrade+"|"+(expedition==null?-1:expedition.Grade));}

  static string HealthText(double multiple){int percent=Mathf.RoundToInt((float)((multiple-1)*100));return percent>0?" · 적 체력 +"+percent+"%":"";}
  static string BattleHealthText(Battle b){return HealthText((1+Boon.ThreatHealth*b.Threat)*(1+Boon.GradeHealth*b.Grade));}
  static string GradeText(int grade){return grade>0?" · 불씨 등급 "+grade:"";}

  // One row: [−]  불씨 등급 N · 적 체력 +X%  [+]. Draws nothing until the first grade is open.
  void GradePicker(Rect row){
   int open=ExpeditionGradeOpen;if(open<=0)return;
   int grade=ExpeditionGrade;float side=row.height;
   Rect minus=new Rect(row.x,row.y,side,side),plus=new Rect(row.xMax-side,row.y,side,side),label=new Rect(row.x+side+8,row.y,row.width-side*2-16,row.height);
   ExpeditionRect("grade-minus",minus);ExpeditionRect("grade-plus",plus);
   if(MenuButton(minus,"-",grade>0))SetExpeditionGrade(grade-1);
   if(MenuButton(plus,"+",grade<open))SetExpeditionGrade(grade+1);
   int oldSize=hud.fontSize;TextAnchor oldAlign=hud.alignment;Color oldColor=hud.normal.textColor;bool oldWrap=hud.wordWrap;
   hud.wordWrap=false;hud.alignment=TextAnchor.MiddleCenter;hud.normal.textColor=grade>0?uiGold:uiText;hud.fontSize=Screen.width>=640&&Screen.height>=420?15:12;
   string text=(grade>0?"불씨 등급 "+grade+" · 모든 전장 적 체력 +"+Mathf.RoundToInt((float)(Boon.GradeHealth*grade*100))+"%":"불씨 등급 0 · 기본 원정")+(grade<open?"":open<Expedition.MaxGrade?" · 이기면 다음 등급":" · 최고 등급");
   while(hud.fontSize>10&&MeasureText(hud,text).x>label.width)hud.fontSize--;
   GUI.Label(label,text,hud);
   hud.fontSize=oldSize;hud.alignment=oldAlign;hud.normal.textColor=oldColor;hud.wordWrap=oldWrap;
  }
 }
}
