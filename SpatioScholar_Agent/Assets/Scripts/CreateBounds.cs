﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CreateBounds : MonoBehaviour
{
    public GameObject perspective;
    public float time_interval = 1;
    public float DRAWING_RADIUS = 0.13f;
    public bool TEST_STRUCTURE = true;
    public bool DEBUG_RAYS = true;

    private List<Dictionary<GameObject, Dictionary<Vector3, bool>>> time_access = new List<Dictionary<GameObject, Dictionary<Vector3, bool>>>();
    private List<Dictionary<GameObject, Dictionary<Vector3, Vector2>>> time_access_textures = new List<Dictionary<GameObject, Dictionary<Vector3, Vector2>>>();
    private Dictionary<GameObject, Bounds> objectBounds = new Dictionary<GameObject, Bounds>();
    private Dictionary<GameObject, Dictionary<Vector3, bool>> objectVectors;
    private Dictionary<GameObject, Dictionary<Vector3, Vector2>> objectVectorTextureCoords; // Only contains texture coords where Vector3 is a hit
    private float elapsed_time = 0f;

    // Start is called before the first frame update
    void Start()
    {
        // Test with sample_targets
        GameObject[] sample_targets = new GameObject[] { GameObject.Find("Intervisibility Target") //GameObject.Find("Layer:3d_monastery_church"), GameObject.Find("Layer:3d_big_cloister_roof"), GameObject.Find("Layer:3d_gothic_cloister"),
                                                           // GameObject.Find("Layer:3d_monastery_fountain")
                                                                };
        DeriveTargetsBounds(sample_targets);
    }

    // Update is called once per frame
    void Update()
    {
        elapsed_time += Time.deltaTime;

        if (perspective.name.Equals("SSAgent")) return; // Don't run when on just the prefab

        if (elapsed_time >= time_interval)
        {
            elapsed_time = elapsed_time % time_interval;

            // Call BoundedRaycast on GameObjects in objects in objectBounds
            List<GameObject> gObjects = new List<GameObject>(objectBounds.Keys);
            Dictionary<GameObject, Dictionary<Vector3, bool>> temp_dict_gObject_vectors;
            Dictionary<GameObject, Dictionary<Vector3, Vector2>> temp_dict_gObject_vector_textures;

            BoundedRaycast(200, 200, 2, gObjects, out temp_dict_gObject_vectors, out temp_dict_gObject_vector_textures);

            // Add returned dictionary to list
            time_access.Add(temp_dict_gObject_vectors);
            time_access_textures.Add(temp_dict_gObject_vector_textures);

            // Test non-timed object coloring
            List<GameObject> textureObjects = new List<GameObject>(objectVectorTextureCoords.Keys);
            if (time_access.Count > 3)
            {
                DrawOnObjectList(textureObjects, 4);
            }

            }

        if (time_access.Count > 4 & TEST_STRUCTURE)
        {
            for (int i = 0; i < time_access.Count; i++)
            {
                print("Accessing Data at Time t=" + i.ToString());

                foreach (KeyValuePair<GameObject, Dictionary<Vector3, bool>> dict in time_access[i])
                {
                    print("KeyValuePairs for GameObject " + dict.Key);

                    foreach (KeyValuePair<Vector3, bool> subset_vector in dict.Value)
                    {

                        print(subset_vector);

                    }
                    print("-----------------------");

                }

            }
            TEST_STRUCTURE = false;
        }

    }

    // Retrieve bounding boxes for each target GameObject provided
    public void DeriveTargetsBounds(GameObject[] targets)
    {
        print("Target(s) Bounds Derived.");

        // Note that Renderer.bounds returns the bounding box enclosing the object in world space by default
        foreach (GameObject target in targets)
        {
            Renderer rend = target.GetComponent<Renderer>();
            objectBounds.Add(target, rend.bounds);
        }

    }

    // Number of subsections x, y, z, to divide along the x, y, z axes, respectively, for each GameObject in targets.
    // Creates new entries in a newly instantiated dictionary. Note x,y,z > 0.
    public void BoundedRaycast(int x, int y, int z, List<GameObject> targets, out Dictionary<GameObject, Dictionary<Vector3, bool>> return1, out Dictionary<GameObject, Dictionary<Vector3, Vector2>> return2)
    {
        print("Intervisibility Bounding Raycast(s) Called");

        // Initialize dictionarys, put all objects in as keys
        objectVectors = new Dictionary<GameObject, Dictionary<Vector3, bool>>();
        objectVectorTextureCoords = new Dictionary<GameObject, Dictionary<Vector3, Vector2>>();
        foreach (GameObject target in targets)
        {
            objectVectors.Add(target, new Dictionary<Vector3, bool>());
            objectVectorTextureCoords.Add(target, new Dictionary<Vector3, Vector2>());
        }

        // Actual content
        foreach (GameObject target in targets)
        {
            print("Starting subdivison of new object: " + target.name);

            float x_interval = (1f / x) * objectBounds[target].size.x;
            float x_start_pos = objectBounds[target].center.x - objectBounds[target].extents.x;
            float x_target = x_start_pos + x_interval * 0.5f;
            float y_interval = (1f / y) * objectBounds[target].size.y;
            float y_start_pos = objectBounds[target].center.y - objectBounds[target].extents.y;
            float y_target = y_start_pos + y_interval * 0.5f;
            float z_interval = (1f / z) * objectBounds[target].size.z;
            float z_start_pos = objectBounds[target].center.z - objectBounds[target].extents.z;
            float z_target = z_start_pos + z_interval * 0.5f;

            for (int i = 0; i < (x); i++)
            {

                for (int j = 0; j < (y); j++)
                {

                    for (int k = 0; k < (z); k++)
                    {

                        // Subdivided target location
                        Vector3 subdivision_target = new Vector3(x_target, y_target, z_target);

                        // Direction vector from camera location to target location
                        Vector3 RayDirection = subdivision_target - perspective.transform.position;

                        // Origin of raycast
                        Vector3 RayCastLocation = perspective.transform.position;

                        RaycastHit hit;
                        if (Physics.Raycast(RayCastLocation, RayDirection, out hit, Mathf.Infinity) && hit.collider.gameObject.name.Equals(target.name))
                        {
                            //print("Ray Hit! Z = " + z_target + "; Y = " + y_target + "; X = " + x_target);
                            if (DEBUG_RAYS) Debug.DrawRay(RayCastLocation, RayDirection, Color.red, 5f);
                            objectVectors[target].Add(subdivision_target, true);
                            objectVectorTextureCoords[target].Add(subdivision_target, hit.textureCoord);
                            //DrawOnObject(target, hit.textureCoord);

                        }
                        else
                        {
                            //print("Ray did not Hit! Z=" + z_target + "; Y = " + y_target + "; X = " + x_target);
                            if (DEBUG_RAYS) Debug.DrawRay(RayCastLocation, RayDirection, Color.black, 5f);
                            objectVectors[target].Add(subdivision_target, false);
                        }

                        z_target = z_target + z_interval;
                        // end of z for-loop
                    }
                    z_target = z_start_pos + z_interval * 0.5f; // reset z_start so it doesn't over-incrememnt on next loop
                    y_target = y_target + y_interval;
                    // end of y for-loop
                }
                y_target = y_start_pos + y_interval * 0.5f; // reset y_start so it doesn't over-incrememnt on next loop
                x_target = x_target + x_interval;
                // end of x for-loop
            }


        }

        return1 = objectVectors;
        return2 = objectVectorTextureCoords;
    }

    // Given a list of GameObjects that have been subdivided, will color those GameObject coordinates which have been hit at int time
    // Currently, time provided must be a multiple of time_interval and nonzero
    // TODO: Create circle drawing methodology that works.
    public void DrawOnObjectList(List<GameObject> targets, int time)
    {
        print("Drawing on GameObjects at time t=" + time);
        time = (int)(time / time_interval) - 1; // convert time provided to index in array
        Color color = Color.red; // set color for marking seen points

        foreach (GameObject target in targets)
        {

            Renderer rend = target.transform.GetComponent<Renderer>();
            MeshCollider meshColl = target.GetComponent<MeshCollider>();

            if (rend == null || rend.sharedMaterial == null || rend.sharedMaterial.mainTexture == null || meshColl == null)
            {
                print("Renderer or MeshCollider or textures of each returned null. Will use Default 1366x768 texture.");
            }

            Texture2D baseTexture = rend.material.mainTexture as Texture2D;
            if (baseTexture == null) { baseTexture = new Texture2D(1366, 768, TextureFormat.ARGB32, false); }

            // Create new texture instance to not overwrite GameObject's texture
            Texture2D tempTexture = new Texture2D(baseTexture.width, baseTexture.height, TextureFormat.ARGB32, false);
            tempTexture.SetPixels32(baseTexture.GetPixels32());

            foreach (KeyValuePair<Vector3, Vector2> pair in time_access_textures[time][target])
            {
                Vector2 pixelUV = pair.Value;
                //print(pixelUV);


                // Creates square at point with DRAWING_RADIUS = 0.5 * (width OR height)
                float adjustedRadX = (DRAWING_RADIUS * tempTexture.width);
                float adjustedRadY = (DRAWING_RADIUS * tempTexture.height);
                float adjustedCX = tempTexture.width * pixelUV.x;
                float adjustedCY = tempTexture.height * pixelUV.y;
                int adjSX = (int)(adjustedCX - 0.5 * adjustedRadX);
                int adjSY = (int)(adjustedCY - 0.5 * adjustedRadY);
                int adjEX = (int)(adjustedCX + 0.5 * adjustedRadX);
                int adjEY = (int)(adjustedCY + 0.5 * adjustedRadY);
                for (int j = adjSX; j < adjEX; j++)
                {
                    for (int i = adjSY; i < adjEY; i++)
                    {
                        tempTexture.SetPixel(j, i, color);
                    }
                }

                /* Following doesn't work yet
                float adjustedRadius = DRAWING_RADIUS * tempTexture.width;
                // Creates circular blotch centered at pixelUV
                for (int j = 0; j <= adjustedRadius; j++)
                {
                    for (int i = 0; i < 360; i++)
                    {
                        double angle = i * System.Math.PI / 180;
                        int x = (int)(pixelUV.x + j * System.Math.Cos(angle));
                        int y = (int)(pixelUV.y + j * System.Math.Sin(angle));

                        tempTexture.SetPixel(x, y, color);
                    }

                }*/

            }

            // Apply texture changes to tempTexture
            tempTexture.Apply();
            // Set GameObject's material.mainTexture to the tempTexture
            rend.material.mainTexture = tempTexture;

        }

    }

    public void DrawOnObject(GameObject target, Vector2 pixelUV) 
    {
        Color color = Color.red;
        Renderer rend = target.transform.GetComponent<Renderer>();
        MeshCollider meshColl = target.GetComponent<MeshCollider>();

        if (rend == null || rend.sharedMaterial == null || rend.sharedMaterial.mainTexture == null || meshColl == null)
        {
            print("Renderer or MeshCollider or textures of each returned null. Will use Default 1366x768 texture.");
        }

        Texture2D baseTexture = rend.material.mainTexture as Texture2D;
        if (baseTexture == null) { baseTexture = new Texture2D(1366, 768, TextureFormat.ARGB32, false); }

        // Create new texture instance to not overwrite GameObject's texture
        Texture2D tempTexture = new Texture2D(baseTexture.width, baseTexture.height, TextureFormat.ARGB32, false);
        tempTexture.SetPixels32(baseTexture.GetPixels32());

            //print(pixelUV);


            // Creates square at point with DRAWING_RADIUS = 0.5 * (width OR height)
            float adjustedRadX = (DRAWING_RADIUS * tempTexture.width);
            float adjustedRadY = (DRAWING_RADIUS * tempTexture.height);
            float adjustedCX = tempTexture.width * pixelUV.x;
            float adjustedCY = tempTexture.height * pixelUV.y;
            int adjSX = (int)(adjustedCX - 0.5 * adjustedRadX);
            int adjSY = (int)(adjustedCY - 0.5 * adjustedRadY);
            int adjEX = (int)(adjustedCX + 0.5 * adjustedRadX);
            int adjEY = (int)(adjustedCY + 0.5 * adjustedRadY);
            for (int j = adjSX; j < adjEX; j++)
            {
                for (int i = adjSY; i < adjEY; i++)
                {
                    tempTexture.SetPixel(j, i, color);
                }
            }

        /* Following doesn't work yet
        float adjustedRadius = DRAWING_RADIUS * tempTexture.width;
        // Creates circular blotch centered at pixelUV
        for (int j = 0; j <= adjustedRadius; j++)
        {
            for (int i = 0; i < 360; i++)
            {
                double angle = i * System.Math.PI / 180;
                int x = (int)(pixelUV.x + j * System.Math.Cos(angle));
                int y = (int)(pixelUV.y + j * System.Math.Sin(angle));

                tempTexture.SetPixel(x, y, color);
            }

        }*/

        // Apply texture changes to tempTexture
        tempTexture.Apply();
        // Set GameObject's material.mainTexture to the tempTexture
        rend.material.mainTexture = tempTexture;


    }


    }
