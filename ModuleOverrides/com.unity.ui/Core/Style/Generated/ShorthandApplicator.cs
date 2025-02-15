// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

/******************************************************************************/
//
//                             DO NOT MODIFY
//          This file has been generated by the UIElementsGenerator tool
//              See ShorthandApplicatorCsGenerator class for details
//
/******************************************************************************/
using System.Collections.Generic;

namespace UnityEngine.UIElements.StyleSheets
{
    internal static partial class ShorthandApplicator
    {
        public static void ApplyBorderColor(StylePropertyReader reader, ref ComputedStyle computedStyle)
        {
            CompileBoxArea(reader, out Color borderTopColor, out Color borderRightColor, out Color borderBottomColor, out Color borderLeftColor);

            computedStyle.visualData.Write().borderTopColor = borderTopColor;
            computedStyle.visualData.Write().borderRightColor = borderRightColor;
            computedStyle.visualData.Write().borderBottomColor = borderBottomColor;
            computedStyle.visualData.Write().borderLeftColor = borderLeftColor;
        }

        public static void ApplyBorderRadius(StylePropertyReader reader, ref ComputedStyle computedStyle)
        {
            CompileBorderRadius(reader, out Length borderTopLeftRadius, out Length borderTopRightRadius, out Length borderBottomRightRadius, out Length borderBottomLeftRadius);

            computedStyle.visualData.Write().borderTopLeftRadius = borderTopLeftRadius;
            computedStyle.visualData.Write().borderTopRightRadius = borderTopRightRadius;
            computedStyle.visualData.Write().borderBottomRightRadius = borderBottomRightRadius;
            computedStyle.visualData.Write().borderBottomLeftRadius = borderBottomLeftRadius;
        }

        public static void ApplyBorderWidth(StylePropertyReader reader, ref ComputedStyle computedStyle)
        {
            CompileBoxArea(reader, out float borderTopWidth, out float borderRightWidth, out float borderBottomWidth, out float borderLeftWidth);

            computedStyle.layoutData.Write().borderTopWidth = borderTopWidth;
            computedStyle.layoutData.Write().borderRightWidth = borderRightWidth;
            computedStyle.layoutData.Write().borderBottomWidth = borderBottomWidth;
            computedStyle.layoutData.Write().borderLeftWidth = borderLeftWidth;
        }

        public static void ApplyFlex(StylePropertyReader reader, ref ComputedStyle computedStyle)
        {
            CompileFlexShorthand(reader, out float flexGrow, out float flexShrink, out Length flexBasis);

            computedStyle.layoutData.Write().flexGrow = flexGrow;
            computedStyle.layoutData.Write().flexShrink = flexShrink;
            computedStyle.layoutData.Write().flexBasis = flexBasis;
        }

        public static void ApplyMargin(StylePropertyReader reader, ref ComputedStyle computedStyle)
        {
            CompileBoxArea(reader, out Length marginTop, out Length marginRight, out Length marginBottom, out Length marginLeft);

            computedStyle.layoutData.Write().marginTop = marginTop;
            computedStyle.layoutData.Write().marginRight = marginRight;
            computedStyle.layoutData.Write().marginBottom = marginBottom;
            computedStyle.layoutData.Write().marginLeft = marginLeft;
        }

        public static void ApplyPadding(StylePropertyReader reader, ref ComputedStyle computedStyle)
        {
            CompileBoxArea(reader, out Length paddingTop, out Length paddingRight, out Length paddingBottom, out Length paddingLeft);

            computedStyle.layoutData.Write().paddingTop = paddingTop;
            computedStyle.layoutData.Write().paddingRight = paddingRight;
            computedStyle.layoutData.Write().paddingBottom = paddingBottom;
            computedStyle.layoutData.Write().paddingLeft = paddingLeft;
        }

        public static void ApplyTransition(StylePropertyReader reader, ref ComputedStyle computedStyle)
        {
            CompileTransition(reader, out List<TimeValue> transitionDelay, out List<TimeValue> transitionDuration, out List<StylePropertyName> transitionProperty, out List<EasingFunction> transitionTimingFunction);

            computedStyle.transitionData.Write().transitionDelay.CopyFrom(transitionDelay);
            computedStyle.transitionData.Write().transitionDuration.CopyFrom(transitionDuration);
            computedStyle.transitionData.Write().transitionProperty.CopyFrom(transitionProperty);
            computedStyle.transitionData.Write().transitionTimingFunction.CopyFrom(transitionTimingFunction);
        }

        public static void ApplyUnityTextOutline(StylePropertyReader reader, ref ComputedStyle computedStyle)
        {
            CompileTextOutline(reader, out Color unityTextOutlineColor, out float unityTextOutlineWidth);

            computedStyle.inheritedData.Write().unityTextOutlineColor = unityTextOutlineColor;
            computedStyle.inheritedData.Write().unityTextOutlineWidth = unityTextOutlineWidth;
        }
    }
}
