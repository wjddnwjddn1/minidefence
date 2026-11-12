using UnityEngine;
namespace EmberBastion {
 public sealed partial class BattleView {
  readonly StoryTypewriter sceneTyping=new StoryTypewriter(),speechTyping=new StoryTypewriter(),barkTyping=new StoryTypewriter();
  readonly System.Collections.Generic.Dictionary<StoryTypewriter,int> typingFrames=new System.Collections.Generic.Dictionary<StoryTypewriter,int>();
  string speechTypingText="",barkTypingText="";float storyTickAt=-99;
  bool TypingReview {get{return Debug.isDebugBuild&&Application.platform==RuntimePlatform.WebGLPlayer&&Application.absoluteURL.Contains("story-type-review=1");}}
  string TypedStory(StoryTypewriter typing,string channel){
   int last;if(Event.current.type==EventType.Repaint&&(!typingFrames.TryGetValue(typing,out last)||last!=Time.frameCount)){typingFrames[typing]=Time.frameCount;int before=typing.Count;bool tick=typing.Tick(Time.unscaledDeltaTime);
    if(tick&&Time.unscaledTime-storyTickAt>=.05f){storyTickAt=Time.unscaledTime;if(sound!=null)sound.Play("typing");}
    if(TypingReview&&before!=typing.Count)Debug.Log("STORY_TYPE|"+channel+"|"+typing.Count+"|"+typing.Total);
   }return typing.Visible;
  }
  void StoryIdleLog(Speaker speaker,int frame){if(TypingReview&&Event.current.type==EventType.Repaint&&debugRibbonLogged.Add("portrait|"+speaker+"|"+frame))Debug.Log("STORY_IDLE|"+speaker+"|"+frame);}
 }
}
