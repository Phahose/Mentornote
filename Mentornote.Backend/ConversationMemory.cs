using System.Collections.Concurrent;
using System.Text;

namespace Mentornote.Backend
{
    public class ConversationMemory
    {
        private readonly ConcurrentDictionary<string, StringBuilder> _sessions = new();

        public void Append(string meetingId, string text)
        {
            var builder = _sessions.GetOrAdd(meetingId, _ => new StringBuilder());
            builder.AppendLine(text);
        }
        public string GetTranscript(string meetingId)
        {
            if (_sessions.ContainsKey(meetingId))
            {
                return _sessions[meetingId].ToString();
            }

            return string.Empty;
        }

        public void Clear(string meetingId)
        {
            _sessions.TryRemove(meetingId, out _);
        }

        public IEnumerable<string> GetAllMeetingIds()
        {
            return _sessions.Keys;
        }
    }
}
