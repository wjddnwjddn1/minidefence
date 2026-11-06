using System;
using UnityEngine;
namespace EmberBastion.PixelFX {
 [Serializable] public sealed class FrameRect { public int x,y,w,h; }
 [Serializable] public sealed class Frame { public FrameRect frame; public int duration; }
 [Serializable] public sealed class Settings { public string id; public float fps; public bool loop; public int frames,columns; public int[] frameSize,pivot; }
 [Serializable] public sealed class Metadata { public Settings pixelfx; }
 [Serializable] public sealed class Sheet { public Frame[] frames; public Metadata meta; }
 public static class SheetReader {
  public static Sheet Read(TextAsset json,Texture2D texture){
   if(json==null||texture==null)throw new ArgumentException("PixelFX requires JSON and PNG");
   var s=JsonUtility.FromJson<Sheet>(json.text);if(s==null||s.frames==null||s.frames.Length==0||s.meta==null||s.meta.pixelfx==null||s.meta.pixelfx.pivot==null||s.meta.pixelfx.pivot.Length!=2)throw new ArgumentException("Invalid PixelFX metadata");
   foreach(var f in s.frames)if(f==null||f.frame==null||f.duration<=0||f.frame.w<=0||f.frame.h<=0||f.frame.x<0||f.frame.y<0||f.frame.x+f.frame.w>texture.width||f.frame.y+f.frame.h>texture.height)throw new ArgumentException("Invalid PixelFX frame");return s;
  }
 }
 [RequireComponent(typeof(SpriteRenderer))]
 public sealed class PixelFxPlayer:MonoBehaviour {
  public Texture2D texture; public TextAsset metadata; public bool playOnEnable=true; public float pixelsPerUnit=100;
  Sprite[] sprites; Sheet sheet; SpriteRenderer output; int frame; float elapsed; bool playing;
  void Awake(){output=GetComponent<SpriteRenderer>();sheet=SheetReader.Read(metadata,texture);sprites=new Sprite[sheet.frames.Length];for(int i=0;i<sprites.Length;i++){var r=sheet.frames[i].frame;var pivot=sheet.meta.pixelfx.pivot;var pos=new Vector2((float)pivot[0]/r.w,1f-(float)pivot[1]/r.h);sprites[i]=Sprite.Create(texture,new Rect(r.x,texture.height-r.y-r.h,r.w,r.h),pos,pixelsPerUnit,0,SpriteMeshType.FullRect);}}
  void OnEnable(){if(playOnEnable&&sprites!=null)Play();} void Start(){if(playOnEnable&&!playing)Play();}
  public void Play(){frame=0;elapsed=0;playing=true;output.enabled=true;output.sprite=sprites[0];}
  void Update(){if(!playing)return;elapsed+=Time.deltaTime;while(playing&&elapsed>=sheet.frames[frame].duration/1000f){elapsed-=sheet.frames[frame].duration/1000f;frame++;if(frame>=sprites.Length){if(sheet.meta.pixelfx.loop)frame=0;else{playing=false;output.enabled=false;break;}}if(playing)output.sprite=sprites[frame];}}
  void OnDestroy(){if(sprites!=null)foreach(var sprite in sprites)if(sprite!=null)Destroy(sprite);}
 }
}
