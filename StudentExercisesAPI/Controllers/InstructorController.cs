using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using System.Data;
using System.Data.SqlClient;
using StudentExercisesAPI.Data;
using Dapper;
using Microsoft.AspNetCore.Http;

namespace StudentExercisesAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class InstructorsController : ControllerBase
    {
        private readonly IConfiguration _config;

        public InstructorsController(IConfiguration config)
        {
            _config = config;
        }

        public IDbConnection Connection
        {
            get
            {
                return new SqlConnection(_config.GetConnectionString("DefaultConnection"));
            }
        }

        // GET api/instructors?q=Taco
        [HttpGet]
        public async Task<IActionResult> Get(string q)
        {
            string sql = @"
            SELECT
                i.Id,
                i.FirstName,
                i.LastName,
                i.SlackHandle,
                i.CohortId,
                c.Id,
                c.Name
            FROM Instructor i
            JOIN Cohort c ON i.CohortId = c.Id
            WHERE 1=1
            ";

            if (q != null)
            {
                string isQ = $@"
                    AND i.FirstName LIKE '%{q}%'
                    OR i.LastName LIKE '%{q}%'
                    OR i.SlackHandle LIKE '%{q}%'
                ";
                sql = $"{sql} {isQ}";
            }

            Console.WriteLine(sql);

            using (IDbConnection conn = Connection)
            {

                IEnumerable<Instructor> instructors = await conn.QueryAsync<Instructor, Cohort, Instructor>(
                    sql,
                    (instructor, cohort) =>
                    {
                        instructor.Cohort = cohort;
                        return instructor;
                    }
                );
                return Ok(instructors);
            }
        }

        // GET api/instructors/5
        [HttpGet("{id}", Name = "GetInstructor")]
        public async Task<IActionResult> Get([FromRoute]int id)
        {
            string sql = $@"
            SELECT
                i.Id,
                i.FirstName,
                i.LastName,
                i.SlackHandle,
                i.CohortId,
                c.Id,
                c.Name
            FROM Instructor i
            JOIN Cohort c ON i.CohortId = c.Id
            WHERE i.Id = {id}
            ";

            using (IDbConnection conn = Connection)
            {
                IEnumerable<Instructor> instructors = await conn.QueryAsync<Instructor, Cohort, Instructor>(
                    sql,
                    (instructor, cohort) =>
                    {
                        instructor.Cohort = cohort;
                        return instructor;
                    }
                );
                return Ok(instructors);
            }
        }

        // POST api/instructors
        [HttpPost]
        public async Task<IActionResult> Post([FromBody] Instructor instructor)
        {
            string sql = $@"INSERT INTO Instructor 
            (FirstName, LastName, SlackHandle, CohortId)
            VALUES
            (
                '{instructor.FirstName}'
                ,'{instructor.LastName}'
                ,'{instructor.SlackHandle}'
                ,{instructor.CohortId}
            );
            SELECT SCOPE_IDENTITY();";

            using (IDbConnection conn = Connection)
            {
                var newId = (await conn.QueryAsync<int>(sql)).Single();
                instructor.Id = newId;
                return CreatedAtRoute("GetStudent", new { id = newId }, instructor);
            }
        }
    }
}