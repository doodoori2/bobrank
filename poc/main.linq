<Query Kind="Program">
  <Reference>&lt;RuntimeDirectory&gt;\System.Linq.dll</Reference>
  <NuGetReference>MathNet.Numerics</NuGetReference>
  <Namespace>MathNet.Numerics.Distributions</Namespace>
</Query>

internal class UserScoreSystem
{
	internal class UserScore
	{
		public long Uid;
		public int Score;
		public System.Nullable<int> LastHighScore;
		
		public UserScore(long uid, int score)
		{
			this.Uid = uid;
			this.Score = score;
			this.LastHighScore = null;
		}
		
		public UserScore(long uid, int score, int lastScore)
		{
			this.Uid = uid;
			this.Score = score;
			this.LastHighScore = lastScore;
		}
	}
	
	private List<UserScore> userScoreData;
	private Dictionary<long, int> userHighScoreData;
	public void GenerateData()
	{
		GenerateGaussianData(10000, 2000);
	}
	
	public IEnumerable<UserScore> GetUserScoreList()
	{
		return userScoreData;
	}

	List<UserScore> GenerateGaussianData(int mean, int std)
	{
		var userScoreList = new List<UserScore>();
		var userHighScoreData = new Dictionary<long, int>();

		// 1,000 users try 1~10 times
		var minTryPerUser = 1;
		var maxTryPerUser = 10;
		for (var i = 1000; i < 2000; i++)
		{
			var r = new Random();
			var uid = i;
			var tryCount = r.Next(minTryPerUser, maxTryPerUser);
			var lastHighScore = int.MinValue;
			for (var j = 0; j < tryCount; j++)
			{
				var score = (int)NextGaussian(mean, std);
				var userScore = (j == 0) ? new UserScore(i, score) : new UserScore(i, score, lastHighScore);
				userScoreList.Add(userScore);
				if(userHighScoreData.ContainsKey(uid) == false || userHighScoreData[uid] < score)
				{
					userHighScoreData[uid] = score;
				}
				lastHighScore = Math.Max(lastHighScore, score);
			}
		}
		this.userScoreData = userScoreList;
		this.userHighScoreData = userHighScoreData;
		return userScoreList;
	}

	private double NextGaussian(float mean, float stdDev)
	{
		MathNet.Numerics.Distributions.Normal normalDist = new Normal(mean, stdDev);
		double randomGaussianValue = normalDist.Sample();
		return randomGaussianValue;
	}

	public int GetLastHighScore(long uid)
	{
		return userHighScoreData[uid];
	}

	public List<long> GetUserList()
	{
		return userHighScoreData.OrderByDescending(x => x.Value).Select(x => x.Key).ToList();
	}

}

public class OrdinalRank
{
	internal Dictionary<long, int> userScoreData = new Dictionary<long, int>();

	internal void AddScore(UserScoreSystem.UserScore userScore)
	{
		AddScore(userScore.Uid, userScore.Score);
	}
	
	public void AddScore(long uid, int score)
	{
		if (userScoreData.ContainsKey(uid) == false)
		{
			userScoreData[uid] = score;
		}
		else
		{
			var lastHighscore = userScoreData[uid];
			if (lastHighscore >= score)
			{
				return;
			}
			userScoreData[uid] = score;
		}
	}

	public int GetRank(int score)
	{
		var rank = 1;
		foreach(var userScore in userScoreData)
		{
			if(userScore.Value > score)
			{
				rank++;
			}
		}
		return rank;
	}
	
	public float GetRankPercent(int score)
	{
		var rank = GetRank(score);
		return 100.0f * rank / userScoreData.Count;
	}
}

public class BobRank
{
	public int SECTION_NUM = 100; 
	public float SECTION_MAX_PROPORTION_FACTOR = 3.0f;

	internal int totalCount = 0;
	internal List<Section> sectionData = new List<Section>(); // TODO sorted list 

	internal class Section
	{
		public int sum;
		public int cnt;
		public float GetAvg()
		{
			return (float)sum / cnt;
		}

		public void AddScore(int score)
		{
			sum += score;
			cnt++;
		}
		public void RemoveScore(int score)
		{
			sum -= score;
			cnt--;
		}
	}

	internal void AddScore(UserScoreSystem.UserScore userScore)
	{
		if(userScore.LastHighScore.HasValue == false)
		{
			AppendScore(userScore.Score);
		}
		else
		{
			if(userScore.LastHighScore.Value >= userScore.Score)
			{
				return;
			}
			if(RemoveScore(userScore.LastHighScore.Value) == false)
			{
				// assert 
				return;
			}
			AppendScore(userScore.Score);			
		}
	}


	private void AppendScore(int score)
	{
		Section section = null;
		if (sectionData.Count < SECTION_NUM)
		{
			section = new Section();
			sectionData.Add(section);
		}
		else
		{
			section = GetNearestSection(score);
		}
		section.AddScore(score);
		totalCount++;
		CheckRebalance(section);
	}

	private bool RemoveScore(int lastScore)
	{
		var section = GetNearestSectionForRemove(lastScore);
		if (section == null)
			return false;
		section.RemoveScore(lastScore);
		totalCount--;
		return true;
	}

	
	public float GetRankPercent(int score)
	{
		var rank = 0;
		foreach(var section in sectionData)
		{
			if(section.GetAvg() >= score)
			{
				rank += section.cnt;
			}
		}
		return 100.0f * rank / totalCount;
	}


	private Section GetNearestSectionForRemove(int score)
	{
		var delta = float.MaxValue;
		Section nearestSection = null;
		foreach (var section in sectionData)
		{
			if(section.cnt == 0)
			{
				continue;
			}
			var dScore = Math.Abs(section.GetAvg() - score);
			// string.Format("section {0} {1}", dScore, delta).Dump();;
			if (dScore <= delta)
			{
				delta = dScore;
				nearestSection = section;
			}
		}
		return nearestSection;
	}

	private Section GetNearestSection(int score, ICollection<Section> exceptSectionList = null)
	{
		var delta = float.MaxValue;
		Section nearestSection = null;
		foreach (var section in sectionData)
		{
			if(exceptSectionList != null && exceptSectionList.Contains(section))
			{
				continue;
			}
			if (section.cnt == 0)
			{
				return section;
			}
			var dScore = Math.Abs(section.GetAvg() - score);
			// string.Format("section {0} {1}", dScore, delta).Dump();;
			if (dScore <= delta)
			{
				delta = dScore;
				nearestSection = section;
			}
		}
		return nearestSection;
	}
	
	private void CheckRebalance(Section section, List<Section> exceptSectionList = null)
	{
		if(totalCount <= SECTION_NUM)
		{
			return;
		}
		// TODO CheckRebalance prob? or condition? 
		
		var criteria = SECTION_MAX_PROPORTION_FACTOR * Math.Max(1, totalCount / SECTION_NUM);
		if(section.cnt <= criteria)
		{
			return;
		}
		// Debug.WriteLine(string.Format("section {0} {1} {2} {3}", section.cnt, criteria, totalCount, SECTION_NUM));
		if (exceptSectionList == null)
		{
			exceptSectionList = new List<Section>();
		}
		exceptSectionList.Add(section);
		var nearSection = GetNearestSection((int)section.GetAvg(), exceptSectionList);
		if (nearSection == null)
		{
//			section.Dump();
			return;
		}
		else
		{
			var moveCnt = (int)(section.cnt / 2);
			var moveSum = (int)(section.GetAvg() * moveCnt);
			// Debug.WriteLine("[{0}] {1} {2} > {3} {4} : {5}", exceptSectionList.Count, section.GetAvg(), section.cnt, nearSection.GetAvg(), nearSection.cnt, moveCnt);
			section.cnt -= moveCnt;
			section.sum -= moveSum;
			if (section.cnt < 0 || section.sum < 0 || nearSection.cnt < 0)
			{
				section.Dump();
				nearSection.Dump();
			}
			nearSection.cnt += moveCnt;
			nearSection.sum += moveSum;
			CheckRebalance(nearSection, exceptSectionList);
		}
	}
}


class Result
{
	public float Avg;
	public int Cnt;
}

void Comparer(UserScoreSystem userScoreSystem, OrdinalRank ordinalRank, BobRank bobRank)
{
	//	ordinalRank.userScoreData.Dump();
	//	bobRank.sectionData.Dump();

	var userList = userScoreSystem.GetUserList();
	// Debug.WriteLine(string.Format("{0}\t{1}\t{2}\t{3}\t{4}", "Uid", "Score", "EstimateRank", "RealRank", "diff"));
	var totalCount = userList.Count();
	var strBuff = new StringBuilder();
	var isFirst = true;
	var sectionSize = 100.0f / bobRank.SECTION_NUM;
	var warningCnt = 0;
	foreach (var userUid in userList)
	{
		var userScore = userScoreSystem.GetLastHighScore(userUid);
		var bobRankResult = bobRank.GetRankPercent(userScore);
		var ordinalRankResult = ordinalRank.GetRankPercent(userScore);
		if(isFirst == false)
		{
			strBuff.Append(",");
		}
		// var s= "{\"score\": \"" + userScore +  "\", \"br\": \"" + bobRankResult + "\", \"or\": \"" + ordinalRankResult + "\"}\n";
		// strBuff.Append(s);
		isFirst = false;
		var diff = (bobRankResult - ordinalRankResult);
		if(diff >= sectionSize)
		{
			warningCnt++;
			// Debug.WriteLine(string.Format("{0}\t{1}\t{2}\t{3}\t{4}", userUid, userScore, bobRankResult, ordinalRankResult, diff));
		}
	}
	Debug.WriteLine(string.Format("Warning: {0} Total:{1}", warningCnt, totalCount));

	var result = new List<Result>();
	foreach (var sec in bobRank.sectionData.OrderByDescending(x => x.GetAvg()).ToList())
	{
		result.Add(new Result() { Avg = sec.GetAvg(), Cnt = sec.cnt });
	}
	result.Dump();
}


void Main()
{
	var bobRank = new BobRank();
	var ordinalRank = new OrdinalRank();
	var userScoreSystem = new UserScoreSystem();
	userScoreSystem.GenerateData();
	var userScoreList = userScoreSystem.GetUserScoreList();

	foreach (var userScore in userScoreList)
	{
		ordinalRank.AddScore(userScore);
	}

	foreach (var userScore in userScoreList)
	{
		bobRank.AddScore(userScore);
	}

	Comparer(userScoreSystem, ordinalRank, bobRank);
}


