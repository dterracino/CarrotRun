﻿using System;
using System.Collections.Generic;
using Assets.Script.GamePlay.Data_Containers;
using Assets.Script.GamePlay.PoolUtilities;
using Assets.Script.ObjectPool;
using UnityEngine;
using Random = System.Random;

namespace Assets.Script.GamePlay
{
	public class BlocksManager : MonoBehaviour
	{
		#region Variables
	    public ProblemFactory ProblemFactoryInstance;
	    public float DistanceProblems;
	    public GameObject[] GroundBlocks;
	    private LinkedList<ProblemContainer> _movableProblems;
	    private LinkedList<GameObject> _movableBlocks;
	    private readonly Random _randome = new Random();
	    public bool MoveGround = true;
		public float GroundMovingScale = 0.5f;
		public Transform StartPosition;
		public Transform EndPosition;
		public Transform GroundPool;
		public Vector3 DistanceBetweenBlocks;
		public Vector3 MovingDirection;
	    private Vector3 _normalizedDirection;
		private Vector3 _nextPosition = Vector3.zero;
		public BlockPrefabs Gp; //Prefabs Collection
		private int _orderOfPlank;
		public int MaxOrderValue = 10000;
		public float Speed;
	    public float StartOffsetX;
		private int _reSpawnCount;
		[Serializable]
		public class BlockPrefabs
		{
			#region  Variables
			public List<Level> Blocks;
            [HideInInspector]
			public int N;

			#endregion
		}
		#endregion

		public void UpdateSpeed(float speed)
		{
			Speed = speed;
			_nextPosition = MovingDirection * speed * -1;
		}
		public void Creator()
		{
		    _normalizedDirection =  new Vector3(1,DistanceBetweenBlocks.y/DistanceBetweenBlocks.x);
		    CreateGround();
		    CreateProblems();
		    SettingStartVeriables();
		}
	    private void CreateProblems()
	    {
	        _movableProblems = new LinkedList<ProblemContainer>();
            //Creating first problem
	        ProblemContainer firstProblemContainer = CreateRandomProblem();

            //Add first problem with offset
	        firstProblemContainer.transform.position = new Vector3(StartPosition.position.x-StartOffsetX, StartPosition.position.y, 0);
	        _movableProblems.AddFirst(firstProblemContainer);
	        AddProblemsToFeatLenght();
	    }

	    private void AddProblemsToFeatLenght()
	    {
	        ProblemContainer lastProblem = _movableProblems.Last.Value;
            if((lastProblem.transform.position.x+lastProblem.Length())<StartPosition.position.x)
                CreateProblem();
	    }

	    private void CreateGround()
	    {
	        _movableBlocks = new LinkedList<GameObject>();
	        ResortGroundPrefabs();
	    }

	    private void SettingStartVeriables()
		{
			UpdateSpeed(GameManager.Instance.SpeedSetting.StartSpeed);
		}

		private void ResortGroundPrefabs()
		{
			GameObject[] planks = GroundBlocks;
			for (var i = 0; i < planks.Length; i++)
			{
				var position = ((DistanceBetweenBlocks) * (i + 1)) + EndPosition.position;
				var plank = planks[i];
				if (plank != null)
				{
					plank.transform.position = position;
				    if (GroundPool != null) plank.transform.parent = GroundPool.transform;
				    planks[i] = plank;
				}
			}
			foreach (GameObject t in planks)
				_movableBlocks.AddLast(t);
			ResortGround();
		}

		public void AddNewProblem()
		{
			if (Gp.Blocks.Count > Gp.N + 1)
				Gp.N++; //Making over problems harder	
		}


	    private ProblemContainer CreateRandomProblem()
	    {
	        Level currentLevel= Gp.Blocks[Gp.N];
	        int randomIndex = _randome.Next(0, currentLevel.Count);
	        ProblemInfo problemInfo = currentLevel[randomIndex];
	        return ProblemFactoryInstance.CreateProblem(problemInfo);
	    }

	    private void CreateProblem()
	    {
	        ProblemContainer problem = CreateRandomProblem();
	        ProblemContainer lastProblem = _movableProblems.Last.Value;
	        float problemLength = lastProblem.Length();
	        Vector3 distance = DistanceProblems*_normalizedDirection;
	        Vector3 newLastPosition = (_normalizedDirection*problemLength) + lastProblem.transform.position + distance;
	        problem.transform.position =newLastPosition;
	        _movableProblems.AddLast(problem);
	    }

	    public void UpdateMovableBlocks()
		{
			var toRemove = new List<ProblemContainer>();
			foreach (ProblemContainer o in _movableProblems)
			{
				o.transform.Translate(_nextPosition * Time.deltaTime);
				if (o.transform.position.x < EndPosition.position.x)
				{
				    ProblemFactoryInstance.DestroyProblem(o.transform);
					toRemove.Add(o);
				}
			}
            //Removing from the linked list
			foreach (var o in toRemove)
			    _movableProblems.Remove(o);

            //Add new problems if it's needed
            AddProblemsToFeatLenght();
        }

        private void UpdateGroundBlocks()
		{
			if (_movableBlocks == null || _movableBlocks.Count < 1)
				return;
			_reSpawnCount = 0;
			for (var listNode = _movableBlocks.First; listNode != null; listNode = listNode.Next)
			{
				GameObject value = listNode.Value;
				value.transform.Translate(_nextPosition * Time.deltaTime * GroundMovingScale);
				if (value.transform.position.x < EndPosition.position.x)
					_reSpawnCount++;
			}
		}

		public void FixedUpdate()
		{

			for (int i = 1; i < _reSpawnCount; i++)
				ReSpawnGround();
			_reSpawnCount = 0;
		}

		public void ReSpawnGround()
		{
			var groundBlock = _movableBlocks.First.Value;
			groundBlock.transform.position = _movableBlocks.Last.Value.transform.position + DistanceBetweenBlocks;
			groundBlock.GetComponentInChildren<SpriteRenderer>().sortingOrder = _orderOfPlank++;
			_movableBlocks.RemoveFirst();
			_movableBlocks.AddLast(groundBlock);
			if (_orderOfPlank > MaxOrderValue)
				ResortGround();
		}

		private void ResortGround()
		{
			_orderOfPlank = 0;
			foreach (GameObject movableBlock in _movableBlocks)
			{
				if (movableBlock != null)
					movableBlock.GetComponentInChildren<SpriteRenderer>().sortingOrder = _orderOfPlank++;
			}
		}

		public void ReSpawnBorder(ProblemContainer item)
		{
			item.Recycle();
			_movableProblems.Remove(item);
		}

		public void BlocksUpdate()
		{
			if (MoveGround)
				UpdateGroundBlocks();
                UpdateMovableBlocks();
		}

		public void RemoveCoin(GameObject item)
		{
			item.SetActive(false);
		}

		public void Reset()
		{
			SettingStartVeriables();
			StopAllCoroutines();
			ResortGround();
			ResumeMobebleBlocksAnimations();
            CreateProblems();
        }

        public void RemoveMovebleProblems()
		{
			foreach (ProblemContainer movableProblem in _movableProblems)
				movableProblem.Recycle();
			_movableProblems =null;
		}

		public void StopAllAnimations()
		{
			if (_movableBlocks != null && _movableProblems != null)
			{
				foreach (GameObject movableBlock in _movableBlocks)
				{
					SAnimation.SAnimation componentInChildren = movableBlock.GetComponentInChildren<SAnimation.SAnimation>();
					componentInChildren?.StopAnimation();
				}
				foreach (ProblemContainer movableProblem in _movableProblems)
				{
					SAnimation.SAnimation componentInChildren = movableProblem.GetComponentInChildren<SAnimation.SAnimation>();
					componentInChildren?.StopAnimation();
				}
			}
		}

		public void ResumeAllAnimations()
		{
			if (_movableBlocks != null && _movableProblems != null)
			{
				ResumeMobebleBlocksAnimations();
				foreach (ProblemContainer movableProblem in _movableProblems)
				{
					SAnimation.SAnimation componentInChildren = movableProblem.GetComponentInChildren<SAnimation.SAnimation>();
					componentInChildren?.StartAnimation();
				}
			}
		}

		private void ResumeMobebleBlocksAnimations()
		{
			foreach (GameObject movableBlock in _movableBlocks)
			{
				SAnimation.SAnimation componentInChildren = movableBlock.GetComponentInChildren<SAnimation.SAnimation>();
				componentInChildren?.StartAnimation();
			}
		}

		public void Stop()
		{
			Speed = 0;
			Gp.N = 0;
			RemoveMovebleProblems();
			StopAllCoroutines();
			StopAllAnimations();
		}

		private float _lastSpeed;
		public void Pause()
		{
			_lastSpeed = Speed;
			UpdateSpeed(0);
		}

		public void UnPause()
		{
			UpdateSpeed(_lastSpeed);
		}

	}
}