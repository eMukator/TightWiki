﻿using System;

namespace TightWiki.Library.DataModels
{
    public class PageToken
    {
        public int PageId { get; set; }
        public string Token { get; set; } = string.Empty;
        public string DoubleMetaphone { get; set; } = string.Empty;
        public double Weight { get; set; }

        public override bool Equals(object? obj)
        {
            return obj is PageToken other
                && PageId == other.PageId
                && string.Equals(Token, other.Token, StringComparison.OrdinalIgnoreCase);
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(PageId, Token.ToLower());
        }
    }
}
