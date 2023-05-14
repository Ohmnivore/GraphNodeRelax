using UnityEditor.Experimental.GraphView;
using UnityEngine.UIElements;

namespace GraphNodeRelax
{
    // The built-in GraphView.Zoomer doesn't execute while the mouse is captured - something that is fixed in GraphToolsFoundation's ContentZoomer.
    // So we resort to reflection.
    class ZoomerProxy
    {
        EventCallback<WheelEvent> m_ZoomerOnWheelEvent;

        public ZoomerProxy(GraphView graphView)
        {
            var zoomerInfo = typeof(GraphView).GetField("m_Zoomer", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
            var zoomer = zoomerInfo.GetValue(graphView);
            var onWheel = zoomer.GetType().GetMethod("OnWheel", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
            m_ZoomerOnWheelEvent = (EventCallback<WheelEvent>)onWheel.CreateDelegate(typeof(EventCallback<WheelEvent>), zoomer);
        }

        public void Invoke(WheelEvent evt)
        {
            m_ZoomerOnWheelEvent.Invoke(evt);
        }
    }
}
