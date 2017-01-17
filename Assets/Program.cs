using UnityEngine;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;

using ViconPegasusSDK.DotNET;


namespace TestUnityVicon
{
	public class Program: MonoBehaviour
	{
		string SubjectName = "";

		ViconPegasusSDK.DotNET.Client MyClient = new ViconPegasusSDK.DotNET.Client();

		public Program()
		{
		}

		void Start()
		{
			print ("Starting...");

            // Make a new client
			Output_GetVersion OGV = MyClient.GetVersion();
			print("GetVersion Major: " + OGV.Major);

			// Connect to a server
			string HostName = "localhost:802";
            int noAttempts = 0;

  			print("Connecting to " + HostName + "...");
            while (!MyClient.IsConnected().Connected)
            {
                // Direct connection
                Output_Connect OC = MyClient.Connect(HostName);
                print("Connect result: " + OC.Result);

                noAttempts += 1;
                if (noAttempts == 3)
                    break;
                System.Threading.Thread.Sleep(200);
            }

			MyClient.EnableSegmentData();
			// get a frame from the data stream so we can inspect the list of subjects
			MyClient.GetFrame();

			Output_GetSubjectCount OGSC = MyClient.GetSubjectCount ();
			print("GetSubjectCount: "+ OGSC.Result + "|" + OGSC.SubjectCount);

			// the first subjects in the data stream will be the original subjects unmodified by pegasus
			Output_GetSubjectName OGSN = MyClient.GetSubjectName(OGSC.SubjectCount - 1);
			print("GetSubjectName: "+ OGSN.Result + "|" + OGSN.SubjectName);

			SubjectName = OGSN.SubjectName;

			// get the position of the root and point the camera at it
			Output_GetSubjectRootSegmentName OGSRSN = MyClient.GetSubjectRootSegmentName(SubjectName);
			Output_GetSegmentGlobalTranslation RootPos = MyClient.GetSegmentGlobalTranslation(SubjectName, OGSRSN.SegmentName);
			
			Vector3 Target = new Vector3(-(float)RootPos.Translation[0], (float)RootPos.Translation[1], (float)RootPos.Translation[2]);
			Camera.main.transform.position = Target;
			Camera.main.transform.Translate( 0, 100, -300 );
			Camera.main.transform.LookAt(Target);
		}

	    void LateUpdate()
		{
			MyClient.GetFrame();

			Output_GetSubjectRootSegmentName OGSRSN = MyClient.GetSubjectRootSegmentName(SubjectName);
			Transform Root = transform.FindChild(OGSRSN.SegmentName);

			ApplyBoneTransform(Root);

			// keep the camera looking at the model
			Output_GetSegmentGlobalTranslation RootPos = MyClient.GetSegmentGlobalTranslation(SubjectName, OGSRSN.SegmentName);
			Vector3 Target = new Vector3(-(float)RootPos.Translation[0], (float)RootPos.Translation[1], (float)RootPos.Translation[2]);
			Camera.main.transform.LookAt(Target);	
		}

		private void ApplyBoneTransform(Transform Bone)
		{
			// update the bone transform from the data stream
			Output_GetSegmentLocalRotationQuaternion ORot = MyClient.GetSegmentLocalRotationQuaternion(SubjectName, Bone.gameObject.name);
			if( ORot.Result == Result.Success )
			{
				Bone.localRotation = new Quaternion(-(float)ORot.Rotation[0], (float)ORot.Rotation[1], (float)ORot.Rotation[2], -(float)ORot.Rotation[3]);
			}
			
			Output_GetSegmentLocalTranslation OTran = MyClient.GetSegmentLocalTranslation(SubjectName, Bone.gameObject.name);	
			if( OTran.Result == Result.Success )
			{
				Bone.localPosition = new Vector3(-(float)OTran.Translation[0]*0.1f, (float)OTran.Translation[1]*0.1f, (float)OTran.Translation[2]*0.1f);
			}

			// recurse through children
			for( int iChild = 0; iChild < Bone.childCount; iChild++ )
			{
				ApplyBoneTransform( Bone.GetChild(iChild) );
			}
		}
	}
}

