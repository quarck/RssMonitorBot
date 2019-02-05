using System;
using System.Collections.Generic;
using System.Text;

namespace Telegram
{
    public class UriBuilder
    {
        private StringBuilder _stringBuilder;
        private bool _isFirstArgument = true;

        public UriBuilder(string baseUri)
        {
            _stringBuilder = new StringBuilder(baseUri);
        }

        public override string ToString() => _stringBuilder.ToString();

        public void AddArgument(string name)
        {
            if (_isFirstArgument)
                _stringBuilder.Append("?");
            else
                _stringBuilder.Append("&");
            _isFirstArgument = false;

            _stringBuilder.Append(Uri.EscapeDataString(name));
        }

        public void AddArgument(string name, string value)
        {
            if (_isFirstArgument)
                _stringBuilder.Append("?");
            else
                _stringBuilder.Append("&");
            _isFirstArgument = false;

            _stringBuilder.Append(Uri.EscapeDataString(name));
            _stringBuilder.Append("=");
            _stringBuilder.Append(Uri.EscapeDataString(value));
        }

        public void AddArgument(string name, long value) => AddArgument(name, value.ToString());
        public void AddArgument(string name, int value) => AddArgument(name, value.ToString());
        public void AddArgument(string name, float value) => AddArgument(name, value.ToString());
        public void AddArgument(string name, double value) => AddArgument(name, value.ToString());
    }
}
