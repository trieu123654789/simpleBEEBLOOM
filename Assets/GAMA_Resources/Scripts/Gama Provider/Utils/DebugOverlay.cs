using System.Collections;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

// EN: On-screen debug log overlay. Captures Unity log messages (Debug.Log, etc.)
//     and displays the most recent N lines on a TextMeshPro UI element.
//     Useful for VR debugging where the console is not visible.
// VI: Overlay hiển thị log debug trên màn hình. Bắt các message log Unity (Debug.Log, v.v.)
//     và hiển thị N dòng gần nhất trên phần tử UI TextMeshPro.
//     Hữu ích cho debug VR khi không thấy console.
public class DebugManager : MonoBehaviour
{
    // EN: UI text component to display log output.
    // VI: Component text UI để hiển thị output log.
    public TMPro.TextMeshProUGUI debugOverlay;
    // EN: Maximum number of log lines to keep in the scrolling buffer.
    // VI: Số dòng log tối đa giữ trong buffer cuộn.
    public int maxLines = 20;
    // EN: FIFO queue of recent log messages.
    // VI: Hàng đợi FIFO của các message log gần đây.
    private Queue<string> queue = new Queue<string>();
    private string currentText = "";

    void OnEnable()
    {
        Application.logMessageReceivedThreaded += HandleLog;
    }

    void OnDisable()
    {
        Application.logMessageReceivedThreaded -= HandleLog;
    }


    void HandleLog(string logString, string stackTrace, LogType type)
    {
        // Delete oldest message
        if (queue.Count >= maxLines) queue.Dequeue();

        queue.Enqueue(logString);

        var builder = new StringBuilder();
        builder.Append("Debug Logs\n\n");
        foreach (string st in queue)
        {
            builder.Append(st).Append("\n");
        }

        currentText = builder.ToString();

        debugOverlay.text = currentText;
    }
}
