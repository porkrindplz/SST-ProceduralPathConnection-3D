using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

[CustomPropertyDrawer(typeof(SeparatorAttribute))]
public class SeparatorDrawer : DecoratorDrawer
{
    public override void OnGUI(Rect position)
    {
        //Get reference to attribute
        SeparatorAttribute separatorAttribute = attribute as SeparatorAttribute;
        //define line to draw
        Rect separatorRect = new Rect(position.xMin,
            position.yMin + separatorAttribute.Spacing,
            position.width,
            separatorAttribute.Height);
        //draw
        EditorGUI.DrawRect(separatorRect, Color.gray);

    }
    public override float GetHeight()
    {
        SeparatorAttribute separatorAttribute = attribute as SeparatorAttribute;

        float totalSpacing = separatorAttribute.Spacing
            + separatorAttribute.Height
            + separatorAttribute.Spacing;

        return totalSpacing;
    }
}
