using System;
using System.IO;
using System.Globalization;
using EmberBastion;
class ReplayStrategy {
 static int Num(string line,string name){return int.Parse(Token(line,name),CultureInfo.InvariantCulture);}
 static string Token(string line,string name){foreach(string part in line.Split(' '))if(part.StartsWith(name+"="))return part.Substring(name.Length+1);throw new Exception("missing "+name+" in "+line);}
 static int Main(string[] args){try{
  string[] lines=File.ReadAllLines(args[0]); int at=0,fields=0;
  while(at<lines.Length){string header=lines[at++];if(!header.StartsWith("WIN "))throw new Exception("unexpected "+header);int field=Num(header,"field"),expectedHP=Num(header,"hp"),expectedKills=Num(header,"kills"),expectedPeak=Num(header,"peak"),expectedTicks=Num(header,"ticks");var b=new Battle(42,field);int tick=0;
   while(at<lines.Length&&!lines[at].StartsWith("WIN ")){
    string line=lines[at];
    if(line.StartsWith("prepare ")){
     while(b.Phase==Phase.Fighting&&tick<=expectedTicks){b.Step(1.0/60);tick++;}
     if(b.Phase!=Phase.Preparation||b.Wave+1!=Num(line,"wave")||b.HP!=Num(line,"hp")||b.Gold!=Num(line,"gold"))throw new Exception("preparation mismatch: "+line);at++;
     while(at<lines.Length){line=lines[at];bool done=false;
      if(line.StartsWith("place "))done=b.Place(Num(line,"kind"),Num(line,"slot"));
      else if(line.StartsWith("upgrade "))done=b.Upgrade(Num(line,"slot"));
      else if(line.StartsWith("branch "))done=b.UpgradeBranch(Num(line,"slot"),Num(line,"branch"));
      else break;
      if(!done||b.Gold!=Num(line,"gold"))throw new Exception("action mismatch: "+line);at++;
     }
     if(!b.Start())throw new Exception("cannot start");continue;
    }
    if(line.StartsWith("ember ")){int when=Num(line,"tick");while(tick<when&&b.Phase==Phase.Fighting){b.Step(1.0/60);tick++;}if(tick!=when||!b.Ember(new Point(double.Parse(Token(line,"x"),CultureInfo.InvariantCulture),double.Parse(Token(line,"y"),CultureInfo.InvariantCulture))))throw new Exception("ember mismatch: "+line);at++;continue;}
    throw new Exception("unexpected "+line);
   }
   while(b.Phase==Phase.Fighting&&tick<=expectedTicks){b.Step(1.0/60);tick++;}
   if(b.Phase!=Phase.Won||b.HP!=expectedHP||tick!=expectedTicks)throw new Exception("final mismatch field="+field+" phase="+b.Phase+" hp="+b.HP+" ticks="+tick);
   if(b.Kills!=expectedKills||b.PeakAlive!=expectedPeak)throw new Exception("combat result mismatch field="+field+" kills="+b.Kills+" peak="+b.PeakAlive);Console.WriteLine("PASS field="+field+" kills="+b.Kills+" peak="+b.PeakAlive+" ticks="+tick);fields++;
  }
  if(fields!=3)throw new Exception("expected three fields");return 0;
 }catch(Exception e){Console.Error.WriteLine(e.Message);return 1;}}
}
