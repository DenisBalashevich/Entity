using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Data.Entity.Infrastructure;
using System.Data.Entity.Validation;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CodeFirstExample
{
    public class Team
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string Coach { get; set; }


        public virtual ICollection<Player> Players { get; set; }
        public Team()
        {
            Players = new List<Player>();
        }
    }
    public class Player
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public int Age { get; set; }

        public virtual ICollection<Team> Teams { get; set; }
        public Player()
        {
            Teams = new List<Team>();
        }
    }

    public class TeamContext : DbContext
    {
        public TeamContext() : base()
        {

        }

        public DbSet<Player> Players { get; set; }
        public DbSet<Team> Teams { get; set; }

        protected override void OnModelCreating(DbModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);
        }

    }
    class Program
    {
        static void Main(string[] args)
        {

            using (var ctx = new TeamContext())
            {
                Player pl = new Player() { Name = "new player" };

                ctx.Players.Add(pl);
                ctx.SaveChanges();

                var query = from b in ctx.Players
                            orderby b.Name
                            select b;

                foreach (var item in query)
                {
                    Console.WriteLine(item.Name);
                }
            }

            // Update

            Player player;
            //1. Get student from DB
            using (var ctx = new TeamContext())
            {
               player = ctx.Players.Where(s => s.Name == "new player").FirstOrDefault<Player>();
            }

            //2. change student name in disconnected mode (out of ctx scope)
            if (player != null)
            {
                player.Name = "Updated Student1";
            }

            //save modified entity using new Context
            using (var dbCtx = new TeamContext())
            {
                //3. Mark entity as modified
                dbCtx.Entry(player).State = System.Data.Entity.EntityState.Modified;

                //4. call SaveChanges
                dbCtx.SaveChanges();

            }


            // Delete

            Player studentToDelete;
            //1. Get student from DB
            using (var ctx = new TeamContext())
            {
                studentToDelete = ctx.Players.Where(s => s.Name == "new player").FirstOrDefault<Player>();
            }

            //Create new context for disconnected scenario
            using (var newContext = new TeamContext())
            {
                newContext.Entry(studentToDelete).State = System.Data.Entity.EntityState.Deleted;

                newContext.SaveChanges();
            }

            //Add graph

            Player newStudent = new Player() {  Name = "New Single Player" };

            //add new course with new teacher into student.courses
            newStudent.Teams.Add(new Team() { Name = "New Team1 for player"});

            using (var context = new TeamContext())
            {
                context.Players.Add(newStudent);
                context.SaveChanges();
            }


            //Concarency

            Player student1WithUser1 = null;
            Player student1WithUser2 = null;

            //User 1 gets student
            using (var context = new TeamContext())
            {
                context.Configuration.ProxyCreationEnabled = false;
                student1WithUser1 = context.Players.Where(s => s.Id == 1).Single();
            }
            //User 2 also get the same student
            using (var context = new TeamContext())
            {
                context.Configuration.ProxyCreationEnabled = false;
                student1WithUser2 = context.Players.Where(s => s.Id == 1).Single();
            }
            //User 1 updates Student name
            student1WithUser1.Name = "Edited from user1";

            //User 2 updates Student name
            student1WithUser2.Name = "Edited from user2";

            using (var context = new TeamContext())
            {
                try
                {
                    context.Entry(student1WithUser1).State = EntityState.Modified;
                    context.SaveChanges();
                }
                catch (DbUpdateConcurrencyException ex)
                {
                    Console.WriteLine("Optimistic Concurrency exception occured");
                }
            }

            //User 2 saves changes after User 1. 
            //User 2 will get concurrency exection 
            //because CreateOrModifiedDate is different in the database 
            using (var context = new TeamContext())
            {
                try
                {
                    context.Entry(student1WithUser2).State = EntityState.Modified;
                    context.SaveChanges();
                }
                catch (DbUpdateConcurrencyException ex)
                {
                    Console.WriteLine("Optimistic Concurrency exception occured");
                }
            }

            //Local data


            using (var ctx = new TeamContext())
            {
                ctx.Players.Load();

                ctx.Players.Add(new Player() {Name = "New Student"});

                var std1 = ctx.Players.Find(1); // find student whose id = 1
                ctx.Players.Remove(std1); // remove student whose id = 1

                var std2 = ctx.Players.Find(2); // find student whose id = 1
                std2.Name = "Modified Name";

                // Loop over the students in context's local.
                Console.WriteLine("In Local: ");
                foreach (var student in ctx.Players.Local)
                {
                    Console.WriteLine("Found {0}: {1} with state {2}",
                        student.Id, student.Name,
                        ctx.Entry(student).State);
                }
            }


            // LINQ Query Syntax:

            using (var context = new TeamContext())
            {
                var res = (from s in context.Players.Include("Team")
                           where s.Name == "Student1"
                           select s).FirstOrDefault<Player>();
            }


            //LINQ Method Syntax:

            using (var ctx = new TeamContext())
            {
                var stud = ctx.Players.Include("Team")
                                   .Where(s => s.Name == "new player").FirstOrDefault<Player>();

            }


            // Load collection


            using (var context = new TeamContext())
            {
                context.Configuration.LazyLoadingEnabled = false;

                var student = (from s in context.Players
                               where s.Name == "new player"
                               select s).FirstOrDefault<Player>();

                context.Entry(student).Collection(s => s.Teams).Load();
            }


            //Validation


            try
            {
                using (var ctx = new TeamContext())
                {
                    ctx.Players.Add(new Player() { Name = "" });
                    ctx.Players.Add(new Player() { Name = "" });

                    ctx.SaveChanges();
                }
            }
            catch (DbEntityValidationException dbEx)
            {
                foreach (DbEntityValidationResult entityErr in dbEx.EntityValidationErrors)
                {
                    foreach (DbValidationError error in entityErr.ValidationErrors)
                    {
                        Console.WriteLine("Error Property Name {0} : Error Message: {1}",
                                            error.PropertyName, error.ErrorMessage);
                    }
                }
            }


            Console.Read();

        }
    }
}
