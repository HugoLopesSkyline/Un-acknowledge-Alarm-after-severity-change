namespace UnacknowledgeAlarmafterseveritychange
{
	using System;
	using System.Collections.Generic;
	using System.Globalization;
	using System.Text;
	using Skyline.DataMiner.Automation;
	using Skyline.DataMiner.Net;
	using Skyline.DataMiner.Net.Messages;

    /// <summary>
    /// Represents a DataMiner Automation script.
    /// </summary>
    public class Script
    {
        /// <summary>
        /// The script entry point.
        /// </summary>
        /// <param name="engine">Link with SLAutomation process.</param>
        public void Run(IEngine engine)
        {
            try
            {
                RunSafe(engine);
            }
            catch (ScriptAbortException)
            {
                // Catch normal abort exceptions (engine.ExitFail or engine.ExitSuccess)
                throw; // Comment if it should be treated as a normal exit of the script.
            }
            catch (ScriptForceAbortException)
            {
                // Catch forced abort exceptions, caused via external maintenance messages.
                throw;
            }
            catch (ScriptTimeoutException)
            {
                // Catch timeout exceptions for when a script has been running for too long.
                throw;
            }
            catch (InteractiveUserDetachedException)
            {
                // Catch a user detaching from the interactive script by closing the window.
                // Only applicable for interactive scripts, can be removed for non-interactive scripts.
                throw;
            }
            catch (Exception e)
            {
                engine.ExitFail("Run|Something went wrong: " + e);
            }
        }

        private void RunSafe(IEngine engine)
        {
            // Retrieve Alarm ID
            ScriptParam paramCorrelationAlarmInfo = engine.GetScriptParam(65006);
            string alarmInfo = paramCorrelationAlarmInfo.Value;
            string[] parts = alarmInfo.Split('|');

            if (!int.TryParse(parts[0], out int alarmID) ||
                !int.TryParse(parts[1], out int dmaID) ||
                !int.TryParse(parts[7], out int severity) ||
                !int.TryParse(parts[9], out int status))
            {
                engine.ExitFail("RunSafe|Failed to parse alarm information.");
                return;
            }

            string ackMotivation = "";
            for (int i = 21; i < parts.Length; i++)
            {
                if (parts[i] == "ALARM.ACKNOWLEDGEMOTIVATION" && i + 1 < parts.Length)
                {
                    ackMotivation = parts[i + 1];
                    break;
                }
            }

            // Create SetAlarmStateMessage
            SetAlarmStateMessage sam = new SetAlarmStateMessage
            {
                AlarmId = alarmID,
                DataMinerID = dmaID,
                Info = new SA(new string[1] { "-1" })
            };

            if (ackMotivation != "Intermittent Alarm")
            {
                sam.State = 3;
                Engine.SLNet.SendMessage(sam);
            }

            if (severity == 5 && status == 25)
            {
                sam.State = 9;  // unmask alarm
                Engine.SLNet.SendMessage(sam);
            }
        }
    }
}