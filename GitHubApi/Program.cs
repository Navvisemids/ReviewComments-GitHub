using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Octokit;
using Octokit.Reactive;

namespace GitHubApi
{
    class Program
    {
        static void Main(string[] args)
        {
            // Read the PAT (Personal Access Token), Organization Name & Repo Name from Command Line
            string orgName = "", repoName = "", githubPAT = "", outputFileName = "";

            // loop through the args
            foreach (string value in args)
            {
                //Console.WriteLine("foreach: {0}", value);
                string[] keyValue = value.Split(":");
                switch (keyValue[0])
                {
                    case "OrgName":
                        orgName = keyValue[1];
                        break;
                    case "RepoName":
                        repoName = keyValue[1];
                        break;
                    case "PAT":
                        githubPAT = keyValue[1];
                        break;
                    case "Output":
                        outputFileName = keyValue[1];
                        break;

                }
            }

            // Validate if any of the mandatory parameters are not sent, if so, then show the error, along with the help text

            if (orgName == "" || repoName == "" || githubPAT == "")
            {
                Console.WriteLine("ERROR: Parameter Missing - GitHub's Organization Name, Repository Name & Personal Access Token are mandatory!!!");
                Console.WriteLine("");
                Console.WriteLine("Usage:");
                Console.WriteLine("     ReviewComments-GitHub OrgName:<Organization Name> RepoName:<Repository Name> PAT:<Personal Access Token> Output:<Output CSV file name>");
                Console.WriteLine("     Output is an optional parameter, and will default to - GitHub_ReviewComents.csv");
                Console.ReadLine();
                return;
            }

            try
            {
                //Initial Console update
                Console.WriteLine("Validated the parameters, good to go...");

                // List to store the Review Comment details
                Console.WriteLine("Connecting to GitHub");
                List<string> reviewComments = new List<string>();
                reviewComments.Add("PullRequest,Title Description,Comment,Developer,Reviewer, FileName,Commented_at");

                // Create the GitHub Object
                var client = new GitHubClient(new ProductHeaderValue(orgName));
                client.Credentials = new Credentials(githubPAT);

                // Get all the review comments
                Console.WriteLine("Reading the PRs from GitHub, this should take few seconds....");
                var comments = client.PullRequest.ReviewComment.GetAllForRepository(orgName, repoName).GetAwaiter().GetResult();

                // Start the loop to create the CSV
                Console.WriteLine("Successfully retrieved the PRs, started writing the review comments to the CSV file...");
                int i = 0;
                foreach (var comment in comments)
                {
                    // Get the PR number
                    var prNumber = comment.PullRequestUrl.Substring(comment.PullRequestUrl.LastIndexOf("/") + 1);

                    // Get the PR details for the PR Title
                    var pr = client.PullRequest.Get(orgName, repoName, Int32.Parse(prNumber)).GetAwaiter().GetResult();

                    //Status Update
                    drawTextProgressBar("Processing", i++, comments.Count);
                    // Append the data to the List
                    reviewComments.Add(string.Format("{0},{1},{2},{3},{4},{5},{6}", RemoveCommas(prNumber), RemoveCommas(pr.Title), RemoveCommas(comment.Body), RemoveCommas(pr.User.Login), RemoveCommas(comment.User.Login), RemoveCommas(comment.Path), RemoveCommas(comment.CreatedAt.DateTime.ToString())));
                }

                // Write to a CSV file
                if(outputFileName.Length == 0 )
                {
                    outputFileName = "GitHub_ReviewComents.csv";
                }
                outputFileName = @".\" + outputFileName;
                System.IO.File.WriteAllLines(outputFileName, reviewComments);

                // Share the final message
                Console.WriteLine("");
                Console.WriteLine("All done....PR Review Comments are written to the CSV!!!!");
            }
            catch(Exception ex)
            {
                Console.WriteLine("CRITICAL ERROR");
                Console.WriteLine("Error Description: " + ex.Message);
            }


        }

        public static void drawTextProgressBar(string stepDescription, int progress, int total)
        {
            int totalChunks = 30;

            //draw empty progress bar
            Console.CursorLeft = 0;
            Console.Write("["); //start
            Console.CursorLeft = totalChunks + 1;
            Console.Write("]"); //end
            Console.CursorLeft = 1;

            double pctComplete = Convert.ToDouble(progress) / total;
            int numChunksComplete = Convert.ToInt16(totalChunks * pctComplete);

            //draw completed chunks
            Console.BackgroundColor = ConsoleColor.Green;
            Console.Write("".PadRight(numChunksComplete));

            //draw incomplete chunks
            Console.BackgroundColor = ConsoleColor.Gray;
            Console.Write("".PadRight(totalChunks - numChunksComplete));

            //draw totals
            Console.CursorLeft = totalChunks + 5;
            Console.BackgroundColor = ConsoleColor.Black;

            string output = progress.ToString() + " of " + total.ToString();
            Console.Write(output.PadRight(15) + stepDescription); //pad the output so when changing from 3 to 4 digits we avoid text shifting
        }

        public static string RemoveCommas(string textToRemove)
        {
            return (textToRemove.Replace(",", " "));
        }
    }
}
