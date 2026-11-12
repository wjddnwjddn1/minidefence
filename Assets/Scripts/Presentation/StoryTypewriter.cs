using System;
using System.Globalization;
namespace EmberBastion {
 // Presentation clock only. Text elements keep surrogate pairs/combining marks intact.
 public sealed class StoryTypewriter {
  const double Rate=65;
  string text="";int[] elements=new int[0];int shown;double clock;
  public int Count {get{return shown;}}public int Total {get{return elements.Length;}}
  public bool Finished {get{return shown>=elements.Length;}}
  public string Visible {get{return Finished?text:text.Substring(0,elements[shown]);}}
  public void Reset(string value){text=value??"";elements=StringInfo.ParseCombiningCharacters(text);shown=0;clock=0;}
  public bool Tick(double delta){if(Finished||delta<=0||double.IsNaN(delta))return false;clock+=Math.Min(delta,.1)*Rate;int before=shown;shown=Math.Min(elements.Length,(int)clock);for(int i=before;i<shown;i++)if(!char.IsWhiteSpace(text,elements[i])&&!char.IsPunctuation(text,elements[i]))return true;return false;}
  public void Complete(){shown=elements.Length;clock=shown;}
 }
}
