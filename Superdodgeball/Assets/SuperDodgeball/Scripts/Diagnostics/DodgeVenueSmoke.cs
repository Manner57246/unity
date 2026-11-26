#if UNITY_EDITOR || DEVELOPMENT_BUILD
using System.Linq;
using UnityEngine;
namespace SuperDodgeball
{
    public sealed partial class DodgeMvpSmoke
    {
        bool venueCheck, laneWarned, laneLive, laneContact, safeContact, allAirborne, airBall, settled;
        int venueLeg, placedLeg;
        float finishAt;
        DodgeInput VenueInput(DodgeActor actor)
        {
            if(App.Page!=DodgePage.Playing || App.CurrentVenue!=DodgeVenue.Flume || actor.transform.position.y<3f)return default;
            // Fire across empty air so ordinary ball hits cannot mask an environmental result.
            var shot=AimAtSign(actor,actor.transform.position+Vector3.right*10+Vector3.up*2);
            shot.pressed|=DodgeButtons.Standard;shot.held=DodgeButtons.Aim;
            return shot;
        }
        void CheckVenues()
        {
            bool host=role=="host";int leg=App.Room.leg;
            if(host && App.Walkable){App.SetPaused(false);App.Controllers[0].RemoteControlled=true;}
            if(host && Time.unscaledTime>=nextState)
            {
                nextState=Time.unscaledTime+.45f;
                System.IO.File.WriteAllText(System.IO.Path.Combine(folder,"host-state.txt"),App.Page+" / "+App.Venue.Displayed+" / "+App.TimeLeft+" / "+venueLeg+"\n"+
                    string.Join("\n",App.Room.seats.Select((s,i)=>i+" "+s.name+" team="+s.team)));
            }
            if(host && App.Page==DodgePage.Lobby && App.CanStart && App.Net.ConnectedHumans==3)
            {
                foreach(var expected in new[]{DodgeVenue.Warehouse,DodgeVenue.Flume,DodgeVenue.Gym,DodgeVenue.Tour})
                    Check(App.HandleLobbyHit(DodgeLobbyAction.Venue,App.Actors[0]) && App.Room.venue==expected,"Host can select "+expected);
                Check(App.StartMatch(),"Begin venue tour");
            }
            if(App.Page==DodgePage.Staging && leg==3 && App.Venue.Displayed==DodgeVenue.Gym)
            {
                if(step==0)
                {
                    Check(laneWarned && laneLive,"Warehouse warnings and rolling parts were visible");
                    Check(allAirborne && airBall,"All five rose and a remote player's airborne throw was visible");
                    Check(settled && !DodgeVenueDirector.InUpdraft && App.Venue.VisibleLoads==0,"Wind ended, players landed and the gym has no active industrial hazards");
                    step=1;finishAt=Time.realtimeSinceStartup+(host?2:0);
                }
                if(Time.realtimeSinceStartup>=finishAt){report.passed=true;Finish();}return;
            }
            if(host && App.Page==DodgePage.Staging && leg<=2 && placedLeg!=leg)
            {
                for(int i=0;i<5;i++)App.PlaceActor(i,App.Clubhouse.CourtSpawn(App.Room.seats[i].team,i),0);
                placedLeg=leg;
            }
            if(App.Page==DodgePage.Playing)
            {
                if(host && venueLeg!=leg)
                {
                    for(int i=0;i<5;i++)
                    {
                        int sign=App.Room.seats[i].team==0?-1:1;
                        App.PlaceActor(i,new Vector3(i==0?0:(i-2)*3f,.05f,leg==1 && i==0?-12:sign*(leg==1?2:10)),sign<0?0:180);
                        var bot=App.Actors[i].GetComponent<DodgeBot>();if(bot && i>=3)bot.enabled=leg==2;
                    }
                    App.ApplyClock(App.Room.duration*.5f+2.6f,0);venueLeg=leg;
                }
                float age=App.Room.duration*.5f-App.TimeLeft;
                if(App.Venue.Displayed==DodgeVenue.Warehouse)
                {
                    laneWarned|=age<0 && App.Venue.Cue!=null;
                    laneLive|=App.Venue.VisibleLoads>0;
                    laneContact|=App.Actors[0].IsStunned;
                    safeContact|=App.Actors[1].IsStunned;
                    if(host && age>10.9f)
                    {
                        Check(App.Venue.RollerHits>0 && laneContact && !safeContact && App.TeamScore(0)+App.TeamScore(1)==0,"Rollers stagger a player in lane without scoring and the gap is safe");
                        App.FinishMatch();finishAt=Time.realtimeSinceStartup+.9f;
                    }
                }
                if(App.Venue.Displayed==DodgeVenue.Flume)
                {
                    allAirborne|=DodgeVenueDirector.InUpdraft && App.Actors.All(a=>a.transform.position.y>3f);
                    airBall|=DodgeVenueDirector.InUpdraft && DodgeBall.Active.Any(b=>b.owner && App.SeatOf(b.owner)>0 && App.SeatOf(b.owner)<3 && b.transform.position.y>4);
                    settled|=age>7.8f && App.Actors.Take(3).All(a=>a.transform.position.y<.65f);
                    if(host && age>9.3f){App.FinishMatch();finishAt=Time.realtimeSinceStartup+.9f;}
                }
            }
            if(host && App.Page==DodgePage.Results && Time.realtimeSinceStartup>=finishAt)
                Check(App.ContinueSeries(),"Next venue through the changing room");
        }
    }
}
#endif
