using System.Collections.Generic;
using UnityEngine;

namespace GraphNodeRelax
{
    static class Algorithm
    {
        // From https://github.com/specoolar/NodeRelax-Blender-Addon/blob/main/operators/utils.py
        public static void Slide(CacheNode node, Vector2 slideVector, float influence, float power)
        {
            if (Mathf.Approximately(0f, influence))
                return;

            if (Mathf.Approximately(0f, power))
                return;

            if (Mathf.Approximately(0f, slideVector.x) && Mathf.Approximately(0f, slideVector.y))
                return;

            var locNode = node.GetPosition(false);
            locNode.position += slideVector * power * influence;
            node.SetPosition(locNode, false);
        }

        // From https://github.com/specoolar/NodeRelax-Blender-Addon/blob/main/operators/utils.py
        public static Vector2 Relax(bool vertical, CacheNode node, float collisionDistance, bool keepOneToOneEdgesStraight)
        {
            #if ALGORITHM_DEBUG
            rectRelax1 ??= new DebugRectElement(Color.blue);
            rectRelax2 ??= new DebugRectElement(Color.green);
            #endif

            var locNode = node.GetPositionInGraph(!vertical);
            if (vertical)
                SwapXY(ref locNode);

            var sumY = 0f;
            var sumCount = 0;
            var inputX = locNode.x;
            var hasInput = false;

            foreach (var input in node.Inputs)
            {
                var locOther = input.GetPositionInGraph(!vertical);
                #if ALGORITHM_DEBUG
                rectRelax1.SetRect(locOther, node);
                #endif
                if (vertical)
                    SwapXY(ref locOther);

                // Align to the right of the right-most input node
                var inputRight = locOther.xMax + collisionDistance;
                if (hasInput)
                    inputX = Mathf.Max(inputX, inputRight);
                else
                    inputX = inputRight;
                hasInput = true;

                if (keepOneToOneEdgesStraight &&
                    node.SingleInputPortY is { } inputPortY &&
                    input.SingleOutputPortY is { } outputPortY)
                {
                    // Align by port
                    sumY += (locOther.y + outputPortY) - inputPortY;
                }
                else
                {
                    // Align by middle
                    sumY += locOther.center.y - locNode.height * 0.5f;
                }

                sumCount++;
            }

            var outputX = locNode.x;
            var hasOutput = false;

            foreach (var output in node.Outputs)
            {
                var locOther = output.GetPositionInGraph(!vertical);
                #if ALGORITHM_DEBUG
                rectRelax1.SetRect(locOther, node);
                #endif
                if (vertical)
                    SwapXY(ref locOther);

                // Align to the left of the left-most output node
                var outputLeft = locOther.x - locNode.width - collisionDistance;
                if (hasOutput)
                    outputX = Mathf.Min(outputX, outputLeft);
                else
                    outputX = outputLeft;
                hasOutput = true;

                if (keepOneToOneEdgesStraight &&
                    node.SingleOutputPortY is { } outputPortY &&
                    output.SingleInputPortY is { } inputPortY)
                {
                    // Align by port
                    sumY += (locOther.y + inputPortY) - outputPortY;
                }
                else
                {
                    // Align by middle
                    sumY += locOther.center.y - locNode.height * 0.5f;
                }

                sumCount++;
            }

            if (sumCount > 0)
            {
                // Average of left-most and right-most alignment
                var inputFactor = hasInput ? 1f : 0f;
                var outputFactor = hasOutput ? 1f : 0f;
                var averageX = inputX * inputFactor + outputX * outputFactor;
                averageX /= inputFactor + outputFactor;

                var averageY = sumY / (float)sumCount;

                var relaxRect = new Rect(outputX, averageY, locNode.width, locNode.height);
                if (vertical)
                    SwapXY(ref relaxRect);
                #if ALGORITHM_DEBUG
                rectRelax2.SetRect(relaxRect, node);
                #endif

                var offsetX = (averageX - locNode.x);
                var offsetY = (averageY - locNode.y);

                if (vertical)
                    return new Vector2(offsetY, offsetX);
                else
                    return new Vector2(offsetX, offsetY);
            }

            return Vector2.zero;
        }

        // From https://github.com/specoolar/NodeRelax-Blender-Addon/blob/main/operators/utils.py
        public static Vector2 Collide(IEnumerable<CacheNode> allNodes, CacheNode node, float collisionDistance)
        {
            var offset = Vector2.zero;
            var numXCollisions = 0;
            var numYCollisions = 0;

            #if ALGORITHM_DEBUG
            rectCollision1 ??= new DebugRectElement(Color.magenta);
            rectCollision2 ??= new DebugRectElement(Color.red);
            #endif

            var locNode = node.GetPositionInGraph(true);

            foreach (var otherNode in allNodes)
            {
                if (otherNode == node)
                    continue;

                var locOther = otherNode.GetPositionInGraph(true);

                var desiredDistance = new Vector2(locNode.width + locOther.width, locNode.height + locOther.height) * 0.5f + new Vector2(collisionDistance, collisionDistance);
                var delta = locOther.center - locNode.center;
                var distance = new Vector2(Mathf.Abs(delta.x), Mathf.Abs(delta.y));
                var intersection = desiredDistance - distance;

                if (intersection.x > 0f && intersection.y > 0f)
                {
                    if (intersection.y < intersection.x)
                    {
                        if (delta.y > 0f)
                            intersection.y *= -1f;

                        var deltaY = intersection.y;

                        offset.y += deltaY;
                        numYCollisions++;

                        locNode.y += deltaY;
                        #if ALGORITHM_DEBUG
                        rectCollision1.SetRect(locOther, node);
                        rectCollision2.SetRect(locNode, node);
                        #endif
                    }
                    else
                    {
                        if (delta.x > 0f)
                            intersection.x *= -1f;

                        var deltaX = intersection.x;

                        offset.x += deltaX;
                        numXCollisions++;

                        locNode.x += deltaX;
                        #if ALGORITHM_DEBUG
                        rectCollision1.SetRect(locOther, node);
                        rectCollision2.SetRect(locNode, node);
                        #endif
                    }
                }
            }

            offset.x /= Mathf.Max(1, numXCollisions);
            offset.y /= Mathf.Max(1, numYCollisions);

            return offset;
        }

        // Swaps the X and Y position and dimensions of the rect (in-place)
        static void SwapXY(ref Rect rect)
        {
            var tempCopy = rect;

            rect.x = tempCopy.y;
            rect.y = tempCopy.x;
            rect.width = tempCopy.height;
            rect.height = tempCopy.width;
        }

#if ALGORITHM_DEBUG
        static DebugRectElement rectRelax1;
        static DebugRectElement rectRelax2;
        static DebugRectElement rectCollision1;
        static DebugRectElement rectCollision2;
        public static DebugRectElement rectBounds;

        public static void ResetDebug()
        {
            rectRelax1?.Reset();
            rectRelax2?.Reset();
            rectCollision1?.Reset();
            rectCollision2?.Reset();
            rectBounds?.Reset();
        }
#endif
    }
}
