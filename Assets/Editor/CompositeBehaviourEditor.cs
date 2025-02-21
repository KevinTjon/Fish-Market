using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

// Creating custom editor for compositebehaviour class
[CustomEditor(typeof(CompositeBehaviour))]

public class CompositeBehaviourEditor : Editor
{
    public override void OnInspectorGUI() //overide the existing unity method to customize inspector
    {
        //setup
        CompositeBehaviour cb = (CompositeBehaviour)target; // casting the target object to CompositeBehaviour       

        // the case where there is no behaviours present
        if(cb.behaviours == null || cb.behaviours.Length == 0)
        {
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.HelpBox("No Behaviours in array.", MessageType.Warning);
            EditorGUILayout.EndHorizontal();            
        }
        else
        {
            // headers
            // NOTE ALL FLOAT NUMBERS ARE ACTUALLY PIXELS IN THIS FILE FOR EDITORGUILAYOUT
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Behaviours", GUILayout.MinWidth(60f), GUILayout.MaxWidth(290f));
            EditorGUILayout.LabelField("Weights", GUILayout.MinWidth(65f), GUILayout.MaxWidth(65f));
            EditorGUILayout.EndHorizontal();

            //instead of constantly checking for changes every frame, use the changecheck() function to detect and changes
            //this allows us to use setdirty() only when changes are being made
            EditorGUI.BeginChangeCheck();

            // creates a field for each behaviour in the arry with its weights
            for(int i = 0; i < cb.behaviours.Length; i++)
            {
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField(i.ToString(), GUILayout.MinWidth(20f), GUILayout.MaxWidth(20f));
                cb.behaviours[i] = (FlockBehaviour)EditorGUILayout.ObjectField(cb.behaviours[i], typeof(FlockBehaviour), false, GUILayout.MinWidth(20f));
                cb.weights[i] = EditorGUILayout.FloatField(cb.weights[i], GUILayout.MinWidth(60f), GUILayout.MaxWidth(60f));
                EditorGUILayout.EndHorizontal();
            }
            //if there was any changes made 
            if(EditorGUI.EndChangeCheck())
            {
                // using setdirty function form unity to save all changes
                EditorUtility.SetDirty(target);
                GUIUtility.ExitGUI();
            }
        }

        //add behavour button
        EditorGUILayout.BeginHorizontal();      
        if(GUILayout.Button("Add Behaviour"))
        {
            AddBehaviour(cb);
            GUIUtility.ExitGUI();
        }

        // uncomment for button layout to be stacked, currently the buttons are side to side.
        /*EditorGUILayout.EndHorizontal();
        EditorGUILayout.BeginHorizontal();*/

        //remove behaviour button
        // only appears if there is exisitng behaviours in the array, else invisible
        if(cb.behaviours != null && cb.behaviours.Length > 0)
        {
            if (GUILayout.Button("Remove Behaviour"))
            {
                RemoveBehaviour(cb);
                GUIUtility.ExitGUI();
            }
        }
        EditorGUILayout.EndHorizontal();
    }

    //add new behaviour slot
    // +1 to the new behaviour and weight array sizes
    void AddBehaviour(CompositeBehaviour cb)
    {
        int oldCount = (cb.behaviours != null) ? cb.behaviours.Length : 0;
        FlockBehaviour[] newBehaviours = new FlockBehaviour[oldCount + 1];
        float[] newWeights = new float[oldCount + 1];

        // existing behaviours and weights to new array
        for(int i = 0; i < oldCount; i++)
        {
            newBehaviours[i] = cb.behaviours[i];
            newWeights[i] = cb.weights[i];
        }
        newWeights[oldCount] = 1f; //default weight incase user does not put weights (dont want 0)
        cb.behaviours = newBehaviours;
        cb.weights = newWeights;
    }

    // remove function
    // just -1 one slot in the new arrays to remove the last behaviour
    void RemoveBehaviour(CompositeBehaviour cb)
    {
        int oldCount = cb.behaviours.Length;
        if(oldCount == 1)
        {
            cb.behaviours = null;
            cb.weights = null;
            return;
        }

        FlockBehaviour[] newbehaviours = new FlockBehaviour[oldCount - 1];
        float[] newWeights = new float[oldCount - 1];

        // exisinting behaviours and weights to new array
        for(int i = 0; i < oldCount -1; i++)
        {
            newbehaviours[i] = cb.behaviours[i];
            newWeights[i] = cb.weights[i];
        }
        cb.behaviours = newbehaviours;
        cb.weights = newWeights;
    }
}