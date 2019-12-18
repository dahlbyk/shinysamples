﻿using System;
using SQLite;


namespace Samples.Models
{
    public class GeofenceEvent
    {
        [PrimaryKey]
        [AutoIncrement]
        public int Id { get; set; }

        public bool Entered { get; set; }
        public DateTime? Reported { get; set; }
        public string Identifier { get; set; }
        public string Source { get; set; }
        public DateTime Date { get; set; }


        public string Text => this.Identifier;
        public string Detail =>
            $"{this.Source} {this.EnteredText} on {this.Date:MMM d 'at' h:mm tt}. {this.ReportedText}";

        private string EnteredText => this.Entered ? "Entered" : "Exited";
        private string ReportedText =>
            this.Reported == null ? "Not reported." : $"Reported on {this.Reported.Value:MMM d 'at' h:mm tt}";
    }
}
