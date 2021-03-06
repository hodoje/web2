﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WebApp.Models.DomainModels;
using WebApp.Persistence.Repository;

namespace WebApp.Persistence.ModelRepositoryInterfaces
{
	public interface IScheduleRepository : IRepository<Schedule, int>
	{
		IEnumerable<Schedule> GetAllIncludeDayOfTheWeek();
	}
}
