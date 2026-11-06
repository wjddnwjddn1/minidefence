#if DEVELOPMENT_BUILD && UNITY_WEBGL
using System;
using Unity.Profiling;
using UnityEngine;
namespace EmberBastion.Presentation {
 internal sealed class AllocationProbe : MonoBehaviour {
  internal static bool Baseline;
  ProfilerRecorder gc; float start,nextReport; long total,maximum; int count,nonzero;
  [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
  static void Boot(){Baseline=Application.absoluteURL.Contains("fx-smoke=alloc-baseline");if(Application.absoluteURL.Contains("fx-smoke=alloc-profile")){var go=new GameObject("AllocationProbe");DontDestroyOnLoad(go);go.AddComponent<AllocationProbe>();}}
  void OnEnable(){gc=ProfilerRecorder.StartNew(ProfilerCategory.Memory,"GC Allocated In Frame");start=Time.realtimeSinceStartup;}
  void LateUpdate(){float elapsed=Time.realtimeSinceStartup-start;if(elapsed<5)return;if(gc.Valid){long value=gc.LastValue;total+=value;maximum=Math.Max(maximum,value);count++;if(value>0)nonzero++;}if(elapsed>=nextReport){nextReport=(float)Math.Floor(elapsed)+1;Debug.Log("ALLOC_HEAP "+(int)elapsed+" "+GC.CollectionCount(0)+" "+UnityEngine.Profiling.Profiler.GetMonoUsedSizeLong());}if(elapsed>=20){Debug.Log("ALLOC_PROFILE "+gc.Valid+" "+count+" "+(count>0?total/count:0)+" "+maximum+" "+nonzero);enabled=false;}}
  void OnDisable(){gc.Dispose();}
 }
}
#endif
