using UnityEditor.Build;
using UnityEditor.Build.Reporting;

namespace DefenseGameEditor
{
	public sealed class AnimationEventMaterialBuildPreprocessor : IPreprocessBuildWithReport, IOrderedCallback
	{
		public int callbackOrder => -1000;

		public void OnPreprocessBuild(BuildReport report)
		{
			AnimationEventMaterialCatalogSync.SyncAll(logSummary: true);
		}
	}
}
