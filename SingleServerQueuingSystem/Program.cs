using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;



namespace SingleServerQueuingSystem
{
    class Program
    {
        const int Q_LIMIT = 100; /* Limit on queue length. */
        const int BUSY = 1;     /* Mnemonics for server's being busy */
        const int IDLE = 0;

        static int next_event_type, num_custs_delayed, num_delays_required, num_events, num_in_q, server_status;

        static float area_num_in_q, area_server_status, mean_interarrival, mean_service, sim_time, time_last_event, total_of_delays;
        static float[] time_arrival = new float[Q_LIMIT + 1], time_next_event = new float[3];


        static void Main(string[] args)
        {
            /* Open input and output files. */

            /* Specify the number of events for the timing function. */
            num_events = 2;

            /* Read input parameters. */
            string input = File.ReadAllText(AppDomain.CurrentDomain.BaseDirectory + "\\mm1.in");
            string[] tokens = input.Split(' ');
            mean_interarrival = float.Parse(tokens[0], CultureInfo.InvariantCulture.NumberFormat);
            mean_service = float.Parse(tokens[1], CultureInfo.InvariantCulture.NumberFormat);
            num_delays_required = Convert.ToInt32(tokens[2]);

            /* Write report heading and input parameters. */
            using (StreamWriter sw = new StreamWriter(AppDomain.CurrentDomain.BaseDirectory + "\\mm1.out", false))
            {
                sw.Write("Single-server queueing system\n\n");
                sw.Write("Mean interarrival time\t" + mean_interarrival.ToString("0.000") + " minutes\n\n");
                sw.Write("Mean service time\t" + mean_service.ToString("0.000") + " minutes\n\n");
                sw.Write("Number of customers\t" + num_delays_required + "\n\n");
            }

            /* Initialize the simulation. */
            initialize();
            /* Run the simulation while more delays are still needed. */
            while (num_custs_delayed < num_delays_required)
            {
                /* Determine the next event. */
                timing();
                /* Update time-average statistical accumulators. */
                update_time_avg_stats();
                /* Invoke the appropriate event function. */
                switch (next_event_type)
                {
                    case 1:
                        arrive();
                        break;
                    case 2:
                        depart();
                        break;
                }
            }
            /* Invoke the report generator and end the simulation. */
            report();
        }
    
        static void initialize() /* Initialization function. */
        {
            /* Initialize the simulation clock. */
            sim_time = 0.0f;
            /* Initialize the state variables. */
            server_status = IDLE;
            num_in_q = 0;
            time_last_event = 0.0f;
            /* Initialize the statistical counters. */
            num_custs_delayed = 0;
            total_of_delays = 0.0f;
            area_num_in_q = 0.0f;
            area_server_status = 0.0f;
            /* Initialize event list.  Since no customers are present, the departure       (service completion) event is eliminated from consideration. */
            time_next_event[1] = sim_time + expon(mean_interarrival);
            time_next_event[2] = 1.0e+30f;
        }
        static void timing() /* Timing function. */
        {
            int i;
            float min_time_next_event = 1.0e+29f;
            next_event_type = 0;
            /* Determine the event type of the next event to occur. */
            for (i = 1; i <= num_events; ++i)
                if (time_next_event[i] < min_time_next_event)
                {
                    min_time_next_event = time_next_event[i];
                    next_event_type = i;
                }
            /* Check to see whether the event list is empty. */
            if (next_event_type == 0)
            {
                /* The event list is empty, so stop the simulation. */
                using (StreamWriter sw = new StreamWriter(AppDomain.CurrentDomain.BaseDirectory + "\\mm1.out", true))
                {
                    sw.Write("\nEvent list empty at time " + sim_time);
                }
                Environment.Exit(0);
            }
            /* The event list is not empty, so advance the simulation clock. */
            sim_time = min_time_next_event;
        }
        static void arrive() /* Arrival event function. */
        {
            float delay;
            /* Schedule next arrival. */
            time_next_event[1] = sim_time + expon(mean_interarrival);
            /* Check to see whether server is busy. */
            if (server_status == BUSY)
            {
                /* Server is busy, so increment number of customers in queue. */
                ++num_in_q;
                /* Check to see whether an overflow condition exists. */
                if (num_in_q > Q_LIMIT)
                {
                    /* The queue has overflowed, so stop the simulation. */
                    using (StreamWriter sw = new StreamWriter(AppDomain.CurrentDomain.BaseDirectory + "\\mm1.out", true))
                    {
                        sw.Write("\nOverflow of the array time_arrival at");
                        sw.WriteLine(" time " + sim_time);
                    }
                    Environment.Exit(0);
                }
                /* There is still room in the queue, so store the time of arrival of the           arriving customer at the (new) end of time_arrival. */
                time_arrival[num_in_q] = sim_time;
            }
            else
            {
                /* Server is idle, so arriving customer has a delay of zero.  (The           following two statements are for program clarity and do not affect           the results of the simulation.) */
                delay = 0.0f;
                total_of_delays += delay;
                /* Increment the number of customers delayed, and make server busy. */
                ++num_custs_delayed;
                server_status = BUSY;
                /* Schedule a departure (service completion). */
                time_next_event[2] = sim_time + expon(mean_service);
            }
        }
        static void depart() /* Departure event function. */
        {
            int i;
            float delay;
            /* Check to see whether the queue is empty. */
            if (num_in_q == 0)
            {
                /* The queue is empty so make the server idle and eliminate the           departure (service completion) event from consideration. */
                server_status = IDLE;
                time_next_event[2] = 1.0e+30f;
            }
            else
            {
                /* The queue is nonempty, so decrement the number of customers in           queue. */
                --num_in_q;
                /* Compute the delay of the customer who is beginning service and update           the total delay accumulator. */
                delay = sim_time - time_arrival[1];
                total_of_delays += delay;
                /* Increment the number of customers delayed, and schedule departure. */
                ++num_custs_delayed;
                time_next_event[2] = sim_time + expon(mean_service);
                /* Move each customer in queue (if any) up one place. */
                for (i = 1; i <= num_in_q; ++i)
                    time_arrival[i] = time_arrival[i + 1];
            }
        }
        static void report() /* Report generator function. */
        {
            /* Compute and write estimates of desired measures of performance. */
            using (StreamWriter sw = new StreamWriter(AppDomain.CurrentDomain.BaseDirectory + "\\mm1.out", true))
            {
                sw.Write("\n\nAverage delay in queue " + (total_of_delays / num_custs_delayed) + "minutes\n\n");
                sw.Write("Average number in queue " + (area_num_in_q / sim_time) + "\n\n");
                sw.Write("Server utilization" + (area_server_status / sim_time) + "\n\n");
                sw.Write("Time simulation ended " + sim_time + "minutes");
            }
        }
        static void update_time_avg_stats() /* Update area accumulators for time-average                                     statistics. */
        {
            float time_since_last_event;
            /* Compute time since last event, and update last-event-time marker. */
            time_since_last_event = sim_time - time_last_event;
            time_last_event = sim_time;
            /* Update area under number-in-queue function. */
            area_num_in_q += num_in_q * time_since_last_event;
            /* Update area under server-busy indicator function. */
            area_server_status += server_status * time_since_last_event;
        }
        static float expon(float mean)
        {  /* Exponential variate generation function. */
            /* Return an exponential random variate with mean "mean". */
            return -mean * (float)Math.Log(LCGrand.lcgrand(1));
        }
    }
}
