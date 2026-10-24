using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Riftrail
{
    /// <summary>Portrait duel layout. All artwork is original temporary geometry, generated at runtime.</summary>
    public sealed partial class BattleView : MonoBehaviour
    {
        private sealed class Effect
        {
            public RectTransform Root;
            public CanvasGroup Alpha;
            public Vector2 Start, End;
            public float Age, Duration, Arc;
            public bool Combat;
        }
        private readonly List<Effect> effects = new List<Effect>();
        private readonly List<UnityEngine.Object> artResources = new List<UnityEngine.Object>();
        private BattleSimulation simulation;
        private BattleSimulation renderedSimulation;
        private CanvasGroup monsterAlpha, contentInteraction;
        private float strikeDuration = .30f;
        private RectTransform safeRoot, contentRoot, arena, hero, sword, monster, slime, guardian;
        private Image slimeBody, guardianBody, healthFill, timerFill;
        private Text stageText, goldText, enemyName, healthText, timerText, stateText;
        private Text bossText, progressText;
        private GameAudio sound;
        private Button bossButton;
        private readonly Image[] milestones = new Image[5];
        private Font font;
        private Sprite disc;
        private int battleId = -1, previousLevel = -1;
        private float clock, strike, hit, levelFlash;
        private int themedArea = -1;
        private Color slimeTint = Hex(0x65B58B), guardianTint = Hex(0x89947A);
        private string areaName = "균열의 숲";
        private Rect lastSafeArea;
        private Vector2Int lastScreen;
        private static readonly Color Ink = Hex(0x203938);
        private static readonly Color Muted = Hex(0x71867E);
        private static readonly Color Cream = Hex(0xF5F0DF);
        private static readonly Color Gold = Hex(0xF6C766);
        private static readonly Color Green = Hex(0x367E64);
        private static readonly Color Red = Hex(0xC36348);
        private static Color Hex(int value) => new Color(((value >> 16) & 255)/255f,((value >> 8)&255)/255f,(value&255)/255f);

        public void Initialize(BattleSimulation simulation, Action challenge, Action reset)
        {
            this.simulation = simulation;
            resetAll=reset;
            sound=GetComponent<GameAudio>();
            font = Resources.Load<Font>("Fonts/NotoSansCJKkr-Regular");
            if(font==null) throw new InvalidOperationException("Korean UI font is missing.");
            disc = CreateDisc();
            var obj = new GameObject("BattleCanvas",typeof(Canvas),typeof(CanvasScaler),typeof(GraphicRaycaster));
            obj.transform.SetParent(transform,false);
            obj.GetComponent<Canvas>().renderMode = RenderMode.ScreenSpaceOverlay;
            var scaler = obj.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1080,1920);
            scaler.matchWidthOrHeight = .5f;
            var canvas = (RectTransform)obj.transform;
            Stretch(Panel(canvas,"Background",Hex(0x102C2C)).rectTransform);
            safeRoot = Rect(canvas,"SafeArea");
            UpdateSafeArea();
            contentRoot = Rect(safeRoot,"Layout");
            contentInteraction = contentRoot.gameObject.AddComponent<CanvasGroup>();
            Anchor(contentRoot,.5f,.5f,1080,1920);
            FitContent();
            arena = Rect(contentRoot,"DuelField");
            Anchor(arena,.5f,.7916667f,1080,800);
            arena.gameObject.AddComponent<RectMask2D>();
            BuildLandscape();
            enemyName = Label(arena,"",34,Cream,.5f,.79f,950,50);
            var healthTrack = Panel(arena,"EnemyHealth",Hex(0x203C37));
            Anchor(healthTrack.rectTransform,.5f,.735f,660,16);
            healthFill = Panel(healthTrack.rectTransform,"HealthFill",Hex(0xE8B568));
            Stretch(healthFill.rectTransform);
            healthText = Label(arena,"",25,Cream,.5f,.695f,800,36);
            var timeTrack = Panel(arena,"BossTimer",new Color(0,0,0,.15f));
            Anchor(timeTrack.rectTransform,.5f,.665f,650,5);
            timerFill = Panel(timeTrack.rectTransform,"TimeRemaining",Red);
            Stretch(timerFill.rectTransform);
            timerText = Label(arena,"",22,Cream,.5f,.615f,980,65);

            hero = Rect(arena,"SwordMaster");
            Anchor(hero,.5f,.5f,220,300);
            BuildHeroLooks();
            monster = Rect(arena,"Enemy");
            Anchor(monster,.5f,.5f,360,350);
            monsterAlpha = monster.gameObject.AddComponent<CanvasGroup>();
            slime = Rect(monster,"SlimeArt"); Stretch(slime);
            slimeBody = BuildSlime(slime);
            guardian = Rect(monster,"GuardianArt"); Stretch(guardian);
            guardianBody = BuildGuardian(guardian);
            BuildMonsterLooks();
            stateText = Label(arena,"",23,Cream,.5f,.14f,960,70);


            progressText = Label(contentRoot,"",23,Hex(0xBDD1B1),.31f,.605f,580,45);
            for (var i=0;i<milestones.Length;i++)
            {
                milestones[i] = Oval(contentRoot,"HuntProgress"+i,Hex(0x38524B),17,17);
                Anchor(milestones[i].rectTransform,.72f+i*.043f,.605f,17,17);
            }
            stageText = Label(contentRoot,"",29,Cream,.265f,.961f,500,84);
            stageText.alignment=TextAnchor.MiddleLeft;

            if (EventSystem.current == null)
                new GameObject("BattleEventSystem",typeof(EventSystem),typeof(StandaloneInputModule)).transform.SetParent(transform,false);
            BuildGrowth();
            bossText = MakeButton(contentRoot,"BossButton",.5f,.5573f,1000,90,Hex(0xA85A3E),challenge,out bossButton);
            bossText.fontSize = 26;
            BuildRebirth();
            BuildMenus();
            BuildRelicReward();
            BuildOfflineReward();
            foreach (var text in arena.GetComponentsInChildren<Text>()) AddTextShadow(text);
            AddTextShadow(stageText);
            AddTextShadow(progressText);
            Render(simulation, false);
        }

        private void BuildLandscape()
        {
            Stretch(Panel(arena,"Sky",Hex(0x4E796A)).rectTransform);
            var sun = Oval(arena,"Sun",Hex(0xB7CDA1),340,340);
            sun.rectTransform.anchoredPosition = new Vector2(325,100);
            var haze = Oval(arena,"Haze",new Color(.82f,.87f,.68f,.2f),550,550);
            haze.rectTransform.anchoredPosition = new Vector2(325,100);
            for (var i=0;i<7;i++)
            {
                var hill = Oval(arena,"DistantHill",Hex(i%2==0?0x416B5E:0x385F53),650,430);
                hill.rectTransform.anchoredPosition = new Vector2(-700+i*250,-80-(i%3)*50);
            }
            for (var i=0;i<8;i++)
            {
                var tree = Rect(arena,"Tree"); Anchor(tree,.5f,.5f,130,350);
                tree.anchoredPosition = new Vector2(-560+i*170,-50-(i%2)*40);
                Box(tree,"Trunk",Hex(0x375A49),0,-60,20,240);
                OvalAt(tree,"Crown",Hex(i%2==0?0x365D4D:0x2D5545),0,30,135,220);
                OvalAt(tree,"CrownLight",Hex(0x3D6551),-25,65,90,140);
            }
            Box(arena,"ForestFloor",Hex(0x284F3F),0,-360,1080,320);
            OvalAt(arena,"DuelPlatform",Hex(0x6F8B5B),10,-262,1030,185);
            OvalAt(arena,"PlatformTop",Hex(0x819764),10,-236,1030,130);
            for (var i=0;i<22;i++)
            {
                var grass = Box(arena,"Grass",Hex(0xA1AF78),-490+(i*157)%980,-278+(i*47)%85,7,17);
                grass.rectTransform.localRotation = Quaternion.Euler(0,0,(i%3-1)*25);
            }
            // The HUD floats over the landscape; text shadows provide local contrast.
        }

        private void BuildHero(RectTransform root)
        {
            OvalAt(root,"Shadow",new Color(0,0,0,.18f),0,-136,205,35);
            var cape = Box(root,"Cape",Hex(0xAE5340),-38,-18,75,155);
            cape.rectTransform.localRotation = Quaternion.Euler(0,0,-17);
            Box(root,"BackBoot",Hex(0x233C40),-34,-112,37,63);
            Box(root,"FrontBoot",Hex(0x233C40),39,-112,39,63);
            OvalAt(root,"Coat",Hex(0x32617A),0,-38,112,125);
            Box(root,"Belt",Hex(0xD9AD62),6,-62,108,17);
            OvalAt(root,"Shoulder",Hex(0x6795A4),-48,1,53,55);
            OvalAt(root,"Face",Hex(0xEAC396),10,72,91,100);
            OvalAt(root,"HairBack",Hex(0xEAC36C),-10,102,104,69);
            var fringe = Box(root,"HairFringe",Hex(0xF6D585),33,109,64,32);
            fringe.rectTransform.localRotation = Quaternion.Euler(0,0,-20);
            Box(root,"Eye",Ink,43,72,10,18);
            Box(root,"Scarf",Hex(0xC9694B),5,25,112,29);
            var tail = Box(root,"ScarfTail",Hex(0xD67D56),-80,22,85,24);
            tail.rectTransform.localRotation = Quaternion.Euler(0,0,-13);
            sword = Rect(root,"SwordArm"); Anchor(sword,.5f,.5f,160,220);
            sword.anchoredPosition = new Vector2(59,-15);
            OvalAt(sword,"Hand",Hex(0xEAC396),5,-9,34,34);
            Box(sword,"Grip",Hex(0x654D38),18,1,16,65);
            Box(sword,"Guard",Hex(0xF0C15F),18,23,73,15);
            var blade = Box(sword,"Blade",Hex(0xE5F2E5),18,110,28,163);
            Box(blade.rectTransform,"BladeEdge",Color.white,7,0,8,163);
            sword.localRotation = Quaternion.Euler(0,0,-18);
        }

        private Image BuildSlime(RectTransform root)
        {
            OvalAt(root,"Shadow",new Color(0,0,0,.18f),0,-133,340,45);
            OvalAt(root,"SlimeLeft",Hex(0x4B9B78),-105,-76,114,105);
            OvalAt(root,"SlimeRight",Hex(0x4B9B78),105,-76,114,105);
            var body = OvalAt(root,"SlimeBody",Hex(0x65B58B),0,-15,295,260);
            OvalAt(root,"Shine",Hex(0x8FD1A0),-58,49,65,45);
            for (var i=0;i<2;i++)
            {
                OvalAt(root,"EyeSocket",Hex(0x294C3E),-62+i*115,-6,46,61);
                OvalAt(root,"Eye",Hex(0xF6DE96),-69+i*115,0,21,33);
                OvalAt(root,"EyeGlint",Cream,-73+i*115,9,8,10);
            }
            Box(root,"Mouth",Hex(0x294C3E),-8,-60,58,15);
            var leaf = OvalAt(root,"Leaf",Hex(0xD5BD69),17,120,42,92);
            leaf.rectTransform.localRotation = Quaternion.Euler(0,0,-35);
            return body;
        }

        private Image BuildGuardian(RectTransform root)
        {
            OvalAt(root,"Shadow",new Color(0,0,0,.20f),0,-133,350,48);
            OvalAt(root,"LeftArm",Hex(0x626F56),-130,-35,85,150);
            OvalAt(root,"RightArm",Hex(0x626F56),130,-35,85,150);
            var body = OvalAt(root,"StoneBody",Hex(0x89947A),0,-18,275,280);
            OvalAt(root,"Moss",Hex(0x557B48),0,84,268,81);
            var horn = Box(root,"LeftHorn",Hex(0xD6C799),-89,130,31,95);
            horn.rectTransform.localRotation = Quaternion.Euler(0,0,25);
            horn = Box(root,"RightHorn",Hex(0xD6C799),89,130,31,95);
            horn.rectTransform.localRotation = Quaternion.Euler(0,0,-25);
            Box(root,"Brow",Hex(0x394E3B),0,35,190,33);
            for (var i=0;i<2;i++) OvalAt(root,"Eye",Hex(0xF5BC64),-53+i*106,26,34,23);
            Box(root,"Jaw",Hex(0x596B50),0,-60,133,48);
            Box(root,"Rune",Hex(0xC9D69B),0,-62,12,30);
            return body;
        }

        public void Render(BattleSimulation s, bool animate = true)
        {
            // Active menus own input, including on the frame their draw order changes.
            contentInteraction.interactable = !IsGrowthMenuOpen;
            contentInteraction.blocksRaycasts = !IsGrowthMenuOpen;
            if (!ReferenceEquals(renderedSimulation, s))
            {
                ClearEffects(false);
                renderedSimulation = s;
                previousLevel = s.UpgradeLevel;
                strike = hit = levelFlash = 0;
            }
            if (SelectedHero != (int)s.Hero) ApplyHeroVisual((int)s.Hero);
            UpdateSafeArea();
            // CanvasScaler can finish after Awake, or change without a new Screen.safeArea.
            FitContent();
            var delta = s.IsPaused || !animate ? 0 : Time.deltaTime;
            clock += delta;
            if (battleId != s.BattleId)
            {
                battleId = s.BattleId;
                strike = hit = 0;
                ClearEffects(true); // Damage numbers must never appear over the next monster.
            }
            if (previousLevel != s.UpgradeLevel)
            {
                if (previousLevel >= 0 && s.UpgradeLevel > previousLevel)
                {
                    levelFlash = .8f;
                    var up = Label(arena,"공격력 증가!",29,Gold,.5f,.5f,260,60);
                    AddEffect(up.rectTransform,new Vector2(-270,100),new Vector2(-270,170),.6f);
                }
                previousLevel = s.UpgradeLevel;
            }
            RenderGrowth(s);
            RenderRebirth(s);
            RenderMenus(s);
            RenderOfflineReward(s);
            ApplyArea(s.Stage);
            var boss = s.Encounter == EncounterKind.Boss;
            stageText.text = $"{s.Stage:00} 스테이지\n{areaName}";
            goldText.text = $"{Number(s.Gold)} 골드";
            enemyName.text = (boss ? (s.IsEliteBoss ? "정예 보스 · " : "보스 · ") : "") + s.Enemy.Name;
            healthText.text = $"체력 {Number(s.Enemy.Health)} / {Number(s.Enemy.MaxHealth)}" + (s.Enemy.Shield>0?$" · 보호막 {Number(s.Enemy.Shield)}":"");
            healthFill.rectTransform.anchorMax = new Vector2((float)(s.Enemy.Health+s.Enemy.Shield)/(s.Enemy.MaxHealth+s.Enemy.MaxShield),1);
            healthFill.color = boss ? Hex(0xE58F66) : Gold;
            timerFill.transform.parent.gameObject.SetActive(boss || s.Enemy.Kind == MonsterKind.Goblin);
            timerFill.rectTransform.anchorMax = new Vector2((float)(boss ? s.BossTimeRemaining/BattleSimulation.BossDuration : s.GoblinTimeRemaining/BattleSimulation.GoblinEscapeTime),1);
            timerFill.color = boss ? Red : Gold;
            timerText.text = MonsterDescription(s);
            stateText.text = s.IsPaused ? "잠시 쉬는 중" : s.Phase == BattlePhase.Escaped ? "고블린이 도망쳤습니다!" : s.Phase == BattlePhase.Defeat ? "시간 초과 · 사냥으로 돌아갑니다"
                : s.Phase == BattlePhase.Victory ? (boss ? "보스 처치! 다음 지역으로 이동합니다" : s.LastKillWasBonus ? "빠른 처치! 골드 두 배" : "골드를 획득했습니다") : HeroDescription(s.Hero);
            progressText.text = boss ? $"보스 보상 +{Number(s.BossReward)} 골드" : $"보스 발견 {s.FarmKills} / {BattleSimulation.RequiredKills}";
            for (var i=0;i<milestones.Length;i++) milestones[i].color = i<s.FarmKills ? Gold : Hex(0x38524B);
            bossButton.interactable = s.CanChallengeBoss || s.CanAbandonBoss;
            bossText.text = boss ? (s.Phase == BattlePhase.Fighting ? "보스 도전 포기 · 일반 사냥으로 돌아가기" : s.Phase==BattlePhase.Victory ? "다음 지역으로 이동 중…" : "도전 실패 · 일반 사냥으로 돌아갑니다")
                : s.FarmKills < BattleSimulation.RequiredKills ? $"몬스터 {BattleSimulation.RequiredKills-s.FarmKills}마리 처치 후 자동 보스 도전" : $"보스 재도전 >\n권장 공격력 {Number(s.BossTargetPower)} · {(s.HasBossPower ? "준비 완료" : "강화가 필요해요")}";
            for (var i=0;i<enemyLooks.Length;i++) enemyLooks[i].gameObject.SetActive(i == (int)s.Enemy.Kind);
            strike = Mathf.Max(0,strike-delta);
            hit = Mathf.Max(0,hit-delta);
            levelFlash = Mathf.Max(0,levelFlash-delta);
            var swing = Mathf.Sin(Mathf.Clamp01(strike/strikeDuration)*Mathf.PI);
            hero.anchoredPosition = new Vector2(-285+swing*(s.Hero == HeroKind.Wuxia ? 120 : s.Hero == HeroKind.Kunoichi ? 65 : 100),-95+Mathf.Sin(clock*2.7f)*3+swing*20);
            var leaving=boss && s.Phase==BattlePhase.Victory;
            if(leaving)
            {
                var travel=Mathf.Clamp01((float)((1.5-s.TransitionRemaining-.2)/1.3));
                hero.anchoredPosition=new Vector2(Mathf.Lerp(-285,900,travel),-95+Mathf.Abs(Mathf.Sin(travel*30))*15);
                stateText.text=s.LastRelicDrop.HasValue?$"{RelicCatalog.Name(s.LastRelicDrop.Value)} 획득!\n다음 지역으로 이동합니다":"보스 처치! 다음 지역으로 이동합니다";
            }
            hero.localRotation = Quaternion.Euler(0,0,leaving?Mathf.Sin(clock*20)*3:-swing*7);
            sword.localRotation = Quaternion.Euler(0,0,-18-swing*(s.Hero == HeroKind.Kunoichi ? 70 : 110));
            var impact = Mathf.Sin(Mathf.Clamp01(hit/.20f)*Mathf.PI);
            monster.anchoredPosition = new Vector2(180+impact*27,-90+Mathf.Sin(clock*2.5f)*6);
            var baseScale = boss ? (s.IsEliteBoss ? 1.18f : 1.08f) : s.Enemy.Kind == MonsterKind.Goblin ? .86f : s.Enemy.Kind == MonsterKind.Orc ? 1 + .05f*s.Enemy.RageLevel : 1;
            var aliveScale = s.Enemy.Health>0 ? 1 : Mathf.Clamp01((float)(s.TransitionRemaining/(boss?1.5:.45)));
            if (s.Enemy.Kind == MonsterKind.Goblin) monster.anchoredPosition += Vector2.down * 19;
            if (s.Phase == BattlePhase.Escaped) monster.anchoredPosition += Vector2.right*(1-(float)(s.TransitionRemaining/.5))*650;
            else if (s.Enemy.Kind == MonsterKind.Goblin && s.EnemyElapsed > 2.2) monster.anchoredPosition += Vector2.right*Mathf.Sin(clock*30)*10;
            monsterAlpha.alpha = s.Phase == BattlePhase.Escaped ? (float)(s.TransitionRemaining/.5) : 1;
            monster.localScale = new Vector3(baseScale*(1+impact*.10f),baseScale*(1-impact*.10f),1)*aliveScale;
            slimeBody.color = hit>0 ? Color.Lerp(slimeTint,Cream,impact*.8f) : slimeTint;
            guardianBody.color = hit>0 ? Color.Lerp(guardianTint,Cream,impact*.8f) : guardianTint;
            golemBody.color = Color.Lerp(Hex(0x98A9AA),Cream,impact*.8f);
            var orcTint = Color.Lerp(Hex(0x81A56B),Hex(0xBF7251),s.Enemy.Kind == MonsterKind.Orc ? s.Enemy.RageLevel/5f : 0);
            orcBody.color = Color.Lerp(orcTint,Cream,impact*.8f);
            if (s.Enemy.Kind == MonsterKind.Golem)
            {
                golemCracks[0].gameObject.SetActive(s.Enemy.Health*3 <= s.Enemy.MaxHealth*2);
                golemCracks[1].gameObject.SetActive(s.Enemy.Health*3 <= s.Enemy.MaxHealth);
            }
            if (s.Enemy.Kind == MonsterKind.AncientGolem)
            {
                ancientCracks[0].gameObject.SetActive(s.Enemy.Health*3 <= s.Enemy.MaxHealth*2);
                ancientCracks[1].gameObject.SetActive(s.Enemy.Health*3 <= s.Enemy.MaxHealth);
            }
            goblinBody.color = Color.Lerp(Hex(0x9EBE77),Cream,impact*.8f);
            RenderExtraMonsters(s,impact);

            RenderRelicReward(delta,ReferenceEquals(s,simulation));
            AnimateEffects(delta);
        }

        public void ShowAttack(BattleAttack attack)
        {
            var firstEffect = effects.Count;
            var heavy = attack.Hero == HeroKind.Wuxia;
            var ninja = attack.Hero == HeroKind.Kunoichi;
            strikeDuration = heavy ? .46f : ninja ? .16f : .30f;
            strike = strikeDuration;
            hit = .20f;
            var attackTint = heavy ? Hex(0x9FE7CF) : ninja ? Hex(0xD7B4EC) : Cream;
            var slash = Box(arena,"Slash",attackTint,100,-40,heavy ? 27 : ninja ? 8 : 12,heavy ? 250 : ninja ? 130 : 190);
            slash.rectTransform.localRotation = Quaternion.Euler(0,0,attack.FollowUp ? 35 : -35);
            AddEffect(slash.rectTransform,new Vector2(75,5),new Vector2(180,-65),.20f);
            var echo = Box(arena,"SlashEcho",attackTint,85,-40,heavy ? 12 : 5,heavy ? 200 : 150);
            echo.rectTransform.localRotation = Quaternion.Euler(0,0,attack.FollowUp ? 35 : -35);
            AddEffect(echo.rectTransform,new Vector2(95,12),new Vector2(200,-58),.24f);
            var text = Label(arena,$"{(attack.Damage==0 ? "회피" : (attack.IsCritical ? "치명타! " : "")+Number(attack.DisplayDamage))}",attack.IsCritical?64:attack.KillingBlow?62:49,attack.IsCritical?Gold:Cream,.5f,.5f,450,95);
            text.fontStyle = FontStyle.Bold;
            text.gameObject.AddComponent<Shadow>().effectColor = new Color(0,0,0,.35f);
            AddEffect(text.rectTransform,new Vector2(attack.FollowUp ? 230 : 140,80),new Vector2(attack.FollowUp ? 230 : 140,175),.65f);
            for (var i=0;i<4;i++)
            {
                var spark = Oval(arena,"ImpactSpark",Gold,10+i*2,10+i*2);
                AddEffect(spark.rectTransform,new Vector2(150,-20),new Vector2(70+i*65,30+(i%2)*60),.3f);
            }
            for (var i=firstEffect;i<effects.Count;i++) effects[i].Combat=true;
        }

        public void ShowGold(long amount)
        {
            var text = Label(arena,$"+{Number(amount)} 골드",37,Gold,.5f,.5f,350,65);
            AddEffect(text.rectTransform,new Vector2(180,-180),new Vector2(180,-80),1.1f);
            for (var i=0;i<5;i++)
            {
                var coin = Oval(arena,"LootCoin",Gold,20,25);
                AddEffect(coin.rectTransform,new Vector2(145+i*20,-120),new Vector2(190,400),.7f+i*.08f,60+i*10);
            }
        }

        private static string HeroDescription(HeroKind hero)
        {
            return hero == HeroKind.Wuxia ? "묵직한 강타 · 두 배 피해 · 골렘에 강함"
                : hero == HeroKind.Kunoichi ? "재빠른 연격 · 두 번 연속 공격" : "안정적인 검술 · 한 번씩 강하게 공격";
        }
        private static string MonsterDescription(BattleSimulation s)
        {
            if (s.Encounter == EncounterKind.Boss)
            {
                var special=ExtraMonsterDescription(s);
                var trait=special ?? (s.Enemy.Kind==MonsterKind.SlimeKing ? "왕의 핵 · 일반 피해와 치명타 적용"
                    : s.Enemy.Kind==MonsterKind.GoblinChief ? "야영지 두목 · 도망가지 않습니다"
                    : s.Enemy.Kind==MonsterKind.OrcChief ? $"족장의 분노 {s.Enemy.RageLevel}/5 · 체력 +{s.Enemy.RageLevel*15}%"
                    : "고대의 갑옷 · 강타 피해 +25%");
                return $"남은 시간 {s.BossTimeRemaining:0.0}초 · {trait}\n보상 +{Number(s.BossReward)} 골드";
            }
            if (s.Enemy.Kind == MonsterKind.Goblin && s.Phase == BattlePhase.Escaped) return "도주 · 보상과 사냥 진행 없음";
            if (s.Enemy.Kind == MonsterKind.Goblin && s.Phase == BattlePhase.Victory)
                return (s.LastKillWasBonus ? "빠른 처치 · 골드 두 배" : "고블린 처치") + $"\n골드 +{Number(s.KillReward)}";
            var reward = $"보상 +{Number(s.KillReward)} 골드";
            var extra=ExtraMonsterDescription(s);
            if(extra!=null) return extra+"\n"+reward;
            switch (s.Enemy.Kind)
            {
                case MonsterKind.Slime: return "말랑한 몸 · 한 타격에 피해 1\n" + reward;
                case MonsterKind.Golem: return "돌의 몸 · 체력 세 배 · 강타 피해 +25%\n" + reward;
                case MonsterKind.Orc: return $"분노 {s.Enemy.RageLevel}/5 · 체력 +{s.Enemy.RageLevel*35}% · 골드 +{s.Enemy.RageLevel*30}%\n" + reward;
                case MonsterKind.Goblin: return $"{s.GoblinTimeRemaining:0.0}초 뒤 도주 · {(s.EnemyElapsed <= BattleSimulation.GoblinBonusTime + 1e-9 ? "빠른 처치 보상 두 배" : "추가 보상 시간 종료")}\n" + reward;
                default: return reward;
            }
        }

        private static string ExtraMonsterDescription(BattleSimulation s)
        {
            var e=s.Enemy;
            if(StageCatalog.IsMushroom(e.Kind)) return e.IsSleeping ? "잠자는 중! 받는 피해 두 배" : "수면 주기 · 2.5~4초 동안 약해집니다";
            if(StageCatalog.IsSkeleton(e.Kind)) return e.Revived ? "부활 완료 · 체력 35%의 마지막 목숨" : "뼈의 부활 · 체력 35%로 한 번 부활";
            if(StageCatalog.IsDokebi(e.Kind)) return $"타격 보상 {e.CoinDrops}/5 · 때릴 때마다 골드";
            if(StageCatalog.IsIce(e.Kind)) return e.Shield>0 ? $"얼음 보호막 {Number(e.Shield)} · 강타 피해 두 배" : "보호막 파괴! 몸이 드러났습니다";
            if(StageCatalog.IsGhost(e.Kind)) return e.IsExposed ? "모습이 드러났습니다 · 공격 기회 1.5초" : "숨어 있는 중 · 공격이 빗나갑니다";
            if(StageCatalog.IsDragon(e.Kind)) return e.IsArmored ? "최후의 방어 · 일반 피해 50%, 치명타 75%" : "용의 비늘 · 체력 30% 이하에서 방어 강화";
            return null;
        }

        public void CancelUpgradeHold()
        {
            foreach(var button in growthButtons) if(button is HoldUpgradeButton held) held.CancelHold();
        }

        private static string Number(long value) => GameNumber.Format(value);

        private void ApplyArea(int stage)
        {
            var area = StageCatalog.RegionIndex(stage);
            if (themedArea == area) return;
            themedArea = area;
            areaName = StageCatalog.Region(stage);
            slimeTint = Hex(0x65B58B); guardianTint = Hex(0x89947A);
            var sky = new[] { Hex(0x4E796A), Hex(0x8B8563), Hex(0x986C59), Hex(0x666D92),Hex(0x8A6C86),Hex(0x657779),Hex(0x85805E),Hex(0x7EACBF),Hex(0x746891),Hex(0xAB8056) }[area];
            var dark = new[] { Hex(0x244B42), Hex(0x4D553C), Hex(0x5E4138), Hex(0x353F5E),Hex(0x503D57),Hex(0x34464C),Hex(0x504C35),Hex(0x3C687F),Hex(0x403652),Hex(0x634931) }[area];
            foreach (var image in arena.GetComponentsInChildren<Image>(true))
            {
                if (image.name == "Sky") image.color = sky;
                if (image.name == "Canopy" || image.name == "ForestFloor") image.color = dark;
                if (image.name == "DistantHill") image.color = Color.Lerp(sky, dark, .5f);
                if (image.name == "SlimeLeft" || image.name == "SlimeRight") image.color = slimeTint * .85f;
            }
        }

        private void AddEffect(RectTransform root,Vector2 start,Vector2 end,float duration,float arc=0)
        {
            root.anchoredPosition = start;
            effects.Add(new Effect { Root=root, Alpha=root.gameObject.AddComponent<CanvasGroup>(), Start=start, End=end, Duration=duration, Arc=arc });
        }
        private void ClearEffects(bool combatOnly)
        {
            for (var i=effects.Count-1;i>=0;i--)
            {
                if (combatOnly && !effects[i].Combat) continue;
                Destroy(effects[i].Root.gameObject);
                effects.RemoveAt(i);
            }
        }
        private void AnimateEffects(float delta)
        {
            for (var i=effects.Count-1;i>=0;i--)
            {
                var e=effects[i]; e.Age+=delta;
                var p=e.Age/e.Duration;
                e.Root.anchoredPosition=Vector2.Lerp(e.Start,e.End,p)+Vector2.up*Mathf.Sin(p*Mathf.PI)*e.Arc;
                e.Alpha.alpha=1-p*p;
                if (p<1) continue;
                Destroy(e.Root.gameObject); effects.RemoveAt(i);
            }
        }
        private void UpdateSafeArea()
        {
            var screen = new Vector2Int(Screen.width,Screen.height);
            if (lastSafeArea==Screen.safeArea && lastScreen==screen) return;
            lastSafeArea=Screen.safeArea; lastScreen=screen;
            safeRoot.anchorMin=new Vector2(lastSafeArea.xMin/Mathf.Max(1,screen.x),lastSafeArea.yMin/Mathf.Max(1,screen.y));
            safeRoot.anchorMax=new Vector2(lastSafeArea.xMax/Mathf.Max(1,screen.x),lastSafeArea.yMax/Mathf.Max(1,screen.y));
            safeRoot.offsetMin=safeRoot.offsetMax=Vector2.zero;
            FitContent();
        }
        private void FitContent()
        {
            if (contentRoot == null) return;
            var scale = Mathf.Min(safeRoot.rect.width/1080f,safeRoot.rect.height/1920f);
            contentRoot.localScale=Vector3.one*Mathf.Max(.01f,scale);
        }
        private void AddTextShadow(Text text)
        {
            var shadow=text.gameObject.AddComponent<Shadow>();
            shadow.effectColor=new Color(.03f,.10f,.08f,.85f);
            shadow.effectDistance=new Vector2(1.5f,-2);
        }
        private Text MakeButton(Transform parent,string name,float x,float y,float width,float height,Color tint,Action action,out Button button)
        {
            var panel=Panel(parent,name,tint); panel.raycastTarget=true;
            Anchor(panel.rectTransform,x,y,width,height);
            button=name == "UpgradeButton" || name.StartsWith("GrowthBuy") ? panel.gameObject.AddComponent<HoldUpgradeButton>() : panel.gameObject.AddComponent<Button>(); button.targetGraphic=panel;
            var colors=button.colors;
            colors.disabledColor=new Color(.65f,.65f,.60f,1);
            colors.pressedColor=new Color(.75f,.85f,.75f,1); button.colors=colors;
            button.onClick.AddListener(()=>{ sound?.Click(); action(); });
            return Label(panel.rectTransform,"",28,Cream,.5f,.5f,width-24,height-10);
        }
        private Text Label(Transform parent,string value,int size,Color tint,float x,float y,float width,float height)
        {
            var rect=Rect(parent,"Label"); Anchor(rect,x,y,width,height);
            var text=rect.gameObject.AddComponent<Text>(); text.font=font;
            text.text=value; text.fontSize=size; text.color=tint;
            text.alignment=TextAnchor.MiddleCenter; text.raycastTarget=false;
            return text;
        }
        private Image Box(Transform parent,string name,Color tint,float x,float y,float w,float h)
        {
            var image=Panel(parent,name,tint); Anchor(image.rectTransform,.5f,.5f,w,h);
            image.rectTransform.anchoredPosition=new Vector2(x,y); return image;
        }
        private Image Oval(Transform parent,string name,Color tint,float w,float h)
        {
            var image=Panel(parent,name,tint); image.sprite=disc;
            Anchor(image.rectTransform,.5f,.5f,w,h); return image;
        }
        private Image OvalAt(Transform parent,string name,Color tint,float x,float y,float w,float h)
        {
            var image=Oval(parent,name,tint,w,h); image.rectTransform.anchoredPosition=new Vector2(x,y); return image;
        }
        private static Image Panel(Transform parent,string name,Color tint)
        { var image=Rect(parent,name).gameObject.AddComponent<Image>(); image.color=tint; image.raycastTarget=false; return image; }
        private static RectTransform Rect(Transform parent,string name)
        { var rect=(RectTransform)new GameObject(name,typeof(RectTransform)).transform; rect.SetParent(parent,false); return rect; }
        private static void Anchor(RectTransform rect,float x,float y,float w,float h)
        { rect.anchorMin=rect.anchorMax=new Vector2(x,y); rect.pivot=new Vector2(.5f,.5f); rect.sizeDelta=new Vector2(w,h); rect.anchoredPosition=Vector2.zero; }
        private static void Stretch(RectTransform rect)
        { rect.anchorMin=Vector2.zero; rect.anchorMax=Vector2.one; rect.offsetMin=rect.offsetMax=Vector2.zero; }
        private Sprite CreateDisc()
        {
            const int size=128;
            var texture=new Texture2D(size,size,TextureFormat.RGBA32,false);
            var pixels=new Color[size*size];
            for (var y=0;y<size;y++) for (var x=0;x<size;x++)
            {
                var distance=Vector2.Distance(new Vector2(x+.5f,y+.5f),Vector2.one*(size/2f));
                pixels[y*size+x]=new Color(1,1,1,Mathf.Clamp01(size/2f-distance));
            }
            texture.SetPixels(pixels); texture.Apply();
            var sprite=Sprite.Create(texture,new UnityEngine.Rect(0,0,size,size),new Vector2(.5f,.5f),100);
            artResources.Add(sprite); artResources.Add(texture);
            return sprite;
        }
        private void OnDestroy()
        { foreach (var resource in artResources) if (resource!=null) Destroy(resource); }
    }
}
