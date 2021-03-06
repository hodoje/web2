﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using WebApp.Models.DomainModels;

namespace WebApp.Models.Dtos
{
    public class ScheduleDto
    {
		public int Id { get; set; }
		public string DayOfTheWeek { get; set; }
		public string Timetable { get; set; }
		public int LineNum { get; set; }
		public string LineType { get; set; }
	}
}