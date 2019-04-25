using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

public class StreetIntersection
{
    [System.Serializable]
    public struct SerializedStreetIntersection
    {
        public SerializableVector3 position;
    }

    /// ID of the intersection.
    public int id;

    /// Position of the intersection.
    public Vector3 position;

    /// Intersecting streets.
    public List<StreetSegment> intersectingStreets;

    public SerializedStreetIntersection Serialize()
    {
        return new SerializedStreetIntersection
        {
            position = new SerializableVector3(position)
        };
    }

    public static void Deserialize(Map map, SerializedStreetIntersection inter)
    {
        map.CreateIntersection(inter.position.ToVector());
    }
}

public class StreetSegment
{
    [System.Serializable]
    public struct SerializedStreetSegment
    {
        public List<SerializableVector3> positions;
        public int startIntersectionID;
        public int endIntersectionID;
    }

    /// ID of the street segment.
    public int id;

    /// The street this segment is part of.
    public Street street;

    /// The position of this segment in the street's segment list.
    public int position;

    /// <summary>
    /// The path of this street segment.
    /// </summary>
    public List<Vector3> positions;

    /// The intersection at the beginning the street.
    public StreetIntersection startIntersection;

    /// The intersection at the end the street.
    public StreetIntersection endIntersection;

    public static readonly float laneWidth = 0.00375f; // 7.5m

    public void Initialize(Street street, int position, List<Vector3> positions,
                           StreetIntersection startIntersection,
                           StreetIntersection endIntersection)
    {
        this.street = street;
        this.position = position;
        this.startIntersection = startIntersection;
        this.endIntersection = endIntersection;

        startIntersection.intersectingStreets.Add(this);
        endIntersection.intersectingStreets.Add(this);

        UpdateMesh(positions);
    }

    float GetStreetWidth(InputController.RenderingDistance distance)
    {
        switch (street.type)
        {
            case Street.Type.Primary:
                switch (distance)
                {
                    case InputController.RenderingDistance.Near:
                        return laneWidth * 2f;
                    case InputController.RenderingDistance.Far:
                    case InputController.RenderingDistance.VeryFar:
                    case InputController.RenderingDistance.Farthest:
                        return laneWidth * 1.8f;
                }

                break;
            case Street.Type.Secondary:
                switch (distance)
                {
                    case InputController.RenderingDistance.Near:
                        return laneWidth * 1.8f;
                    case InputController.RenderingDistance.Far:
                    case InputController.RenderingDistance.VeryFar:
                        return laneWidth * 1.6f;
                    case InputController.RenderingDistance.Farthest:
                        return 0f;
                }

                break;
            case Street.Type.Tertiary:
            case Street.Type.Residential:
                switch (distance)
                {
                    case InputController.RenderingDistance.Near:
                        return laneWidth * 1.4f;
                    case InputController.RenderingDistance.Far:
                        return laneWidth * 1.25f;
                    case InputController.RenderingDistance.VeryFar:
                    case InputController.RenderingDistance.Farthest:
                        return 0f;
                }

                break;
            case Street.Type.Path:
                switch (distance)
                {
                    case InputController.RenderingDistance.Near:
                    case InputController.RenderingDistance.Far:
                        return laneWidth;
                    case InputController.RenderingDistance.VeryFar:
                    case InputController.RenderingDistance.Farthest:
                        return 0f;
                }

                break;
            case Street.Type.River:
                switch (distance)
                {
                    case InputController.RenderingDistance.Near:
                        return laneWidth * 2.2f;
                    case InputController.RenderingDistance.Far:
                        return laneWidth * 2f;
                    case InputController.RenderingDistance.VeryFar:
                        return laneWidth * 1.8f;
                    case InputController.RenderingDistance.Farthest:
                        return 0f;
                }

                break;
            default:
                break;
        }

        throw new System.ArgumentException(string.Format("Illegal enum value {0}", distance));
    }

    float GetBorderWidth(InputController.RenderingDistance distance)
    {
        switch (street.type)
        {
            case Street.Type.Primary:
                switch (distance)
                {
                    case InputController.RenderingDistance.Near:
                        return 0.001f;
                    case InputController.RenderingDistance.Far:
                        return 0.003f;
                    case InputController.RenderingDistance.VeryFar:
                    case InputController.RenderingDistance.Farthest:
                        return 0f;
                }

                break;
            case Street.Type.Secondary:
                switch (distance)
                {
                    case InputController.RenderingDistance.Near:
                        return 0.001f;
                    case InputController.RenderingDistance.Far:
                    case InputController.RenderingDistance.VeryFar:
                        return 0.003f;
                    case InputController.RenderingDistance.Farthest:
                        return 0f;
                }

                break;
            case Street.Type.Tertiary:
            case Street.Type.Residential:
                switch (distance)
                {
                    case InputController.RenderingDistance.Near:
                        return 0.001f;
                    case InputController.RenderingDistance.Far:
                        return 0.003f;
                    case InputController.RenderingDistance.VeryFar:
                    case InputController.RenderingDistance.Farthest:
                        return 0f;
                }

                break;
            case Street.Type.Path:
                switch (distance)
                {
                    case InputController.RenderingDistance.Near:
                    case InputController.RenderingDistance.Far:
                    case InputController.RenderingDistance.VeryFar:
                    case InputController.RenderingDistance.Farthest:
                        return 0f;
                }

                break;
            case Street.Type.River:
                switch (distance)
                {
                    case InputController.RenderingDistance.Near:
                        return 0.002f;
                    case InputController.RenderingDistance.Far:
                        return 0.003f;
                    case InputController.RenderingDistance.VeryFar:
                    case InputController.RenderingDistance.Farthest:
                        return 0f;
                }

                break;
            default:
                break;
        }

        throw new System.ArgumentException(string.Format("Illegal enum value {0}", distance));
    }

    Color GetStreetColor(InputController.RenderingDistance distance)
    {
        switch (street.type)
        {
            case Street.Type.Primary:
                switch (distance)
                {
                    case InputController.RenderingDistance.Near:
                    case InputController.RenderingDistance.Far:
                        return Color.white;
                    case InputController.RenderingDistance.VeryFar:
                    case InputController.RenderingDistance.Farthest:
                        return new Color(0.7f, 0.7f, 0.7f);
                }

                break;
            case Street.Type.Secondary:
                switch (distance)
                {
                    case InputController.RenderingDistance.Near:
                    case InputController.RenderingDistance.Far:
                        return Color.white;
                    case InputController.RenderingDistance.VeryFar:
                    case InputController.RenderingDistance.Farthest:
                        return new Color(0.7f, 0.7f, 0.7f);
                }

                break;
            case Street.Type.Tertiary:
            case Street.Type.Residential:
                switch (distance)
                {
                    case InputController.RenderingDistance.Near:
                    case InputController.RenderingDistance.Far:
                    case InputController.RenderingDistance.VeryFar:
                    case InputController.RenderingDistance.Farthest:
                        return Color.white;
                }

                break;
            case Street.Type.Path:
                switch (distance)
                {
                    case InputController.RenderingDistance.Near:
                    case InputController.RenderingDistance.Far:
                    case InputController.RenderingDistance.VeryFar:
                    case InputController.RenderingDistance.Farthest:
                        return new Color(92f / 255f, 92f / 255f, 87f / 255f);
                }

                break;
            case Street.Type.River:
                switch (distance)
                {
                    case InputController.RenderingDistance.Near:
                    case InputController.RenderingDistance.Far:
                    case InputController.RenderingDistance.VeryFar:
                    case InputController.RenderingDistance.Farthest:
                        return new Color(160f / 255f, 218f / 255f, 242f / 255f);
                }

                break;
            default:
                break;
        }

        throw new System.ArgumentException(string.Format("Illegal enum value {0}", distance));
    }

    Color GetBorderColor(InputController.RenderingDistance distance)
    {
        switch (street.type)
        {
            case Street.Type.Primary:
                switch (distance)
                {
                    case InputController.RenderingDistance.Near:
                    case InputController.RenderingDistance.Far:
                    case InputController.RenderingDistance.VeryFar:
                    case InputController.RenderingDistance.Farthest:
                        return Color.gray;
                }

                break;
            case Street.Type.Secondary:
                switch (distance)
                {
                    case InputController.RenderingDistance.Near:
                    case InputController.RenderingDistance.Far:
                    case InputController.RenderingDistance.VeryFar:
                    case InputController.RenderingDistance.Farthest:
                        return Color.gray;
                }

                break;
            case Street.Type.Tertiary:
            case Street.Type.Residential:
                switch (distance)
                {
                    case InputController.RenderingDistance.Near:
                    case InputController.RenderingDistance.Far:
                    case InputController.RenderingDistance.VeryFar:
                    case InputController.RenderingDistance.Farthest:
                        return Color.gray;
                }

                break;
            case Street.Type.Path:
                switch (distance)
                {
                    case InputController.RenderingDistance.Near:
                    case InputController.RenderingDistance.Far:
                    case InputController.RenderingDistance.VeryFar:
                    case InputController.RenderingDistance.Farthest:
                        return new Color(0f, 0f, 0f, 0f);
                }

                break;
            case Street.Type.River:
                switch (distance)
                {
                    case InputController.RenderingDistance.Near:
                    case InputController.RenderingDistance.Far:
                    case InputController.RenderingDistance.VeryFar:
                    case InputController.RenderingDistance.Farthest:
                        return new Color(116f/255f, 187f/255f, 218f/255f);
                }

                break;
            default:
                break;
        }

        throw new System.ArgumentException(string.Format("Illegal enum value {0}", distance));
    }

    void CreateMeshes()
    {
        float lineLayer;
        float lineOutlineLayer;

        if (street.type == Street.Type.River)
        {
            lineLayer = Map.Layer(MapLayer.Rivers);
            lineOutlineLayer = Map.Layer(MapLayer.RiverOutlines);
        }
        else
        {
            lineLayer = Map.Layer(MapLayer.Streets);
            lineOutlineLayer = Map.Layer(MapLayer.StreetOutlines);
        }

        foreach (var dist in Enum.GetValues(typeof(InputController.RenderingDistance)))
        {
            street.map.streetMesh.AddStreetSegment(
                (InputController.RenderingDistance)dist,
                positions,
                GetStreetWidth((InputController.RenderingDistance)dist),
                GetBorderWidth((InputController.RenderingDistance)dist),
                GetStreetColor((InputController.RenderingDistance)dist),
                GetBorderColor((InputController.RenderingDistance)dist),
                startIntersection.intersectingStreets.Count == 1,
                endIntersection.intersectingStreets.Count == 1,
                lineLayer, lineOutlineLayer);
        }
    }

    public void UpdateMesh(List<Vector3> positions)
    {
        this.positions = positions;
        CreateMeshes();
    }

    public SerializedStreetSegment Serialize()
    {
        return new SerializedStreetSegment
        {
            positions = positions.Select(p => new SerializableVector3(p)).ToList(),
            startIntersectionID = startIntersection?.id ?? 0,
            endIntersectionID = endIntersection?.id ?? 0,
        };
    }
}
