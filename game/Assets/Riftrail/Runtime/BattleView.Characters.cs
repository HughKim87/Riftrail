using System;
using UnityEngine;
using UnityEngine.UI;

namespace Riftrail
{
    public sealed partial class BattleView
    {
        private static readonly string[] HeroNames = { "검사", "무협 검객", "여자 닌자" };
        private readonly RectTransform[] heroLooks = new RectTransform[3];
        private readonly RectTransform[] heroWeapons = new RectTransform[3];
        private RectTransform[] enemyLooks;
        private Image golemBody, orcBody, goblinBody;
        private RectTransform selectionRoot;
        private RectTransform[] golemCracks, ancientCracks;
        public int SelectedHero { get; private set; }
        public bool IsChoosingHero => selectionRoot != null && selectionRoot.gameObject.activeSelf;

        private void BuildHeroLooks()
        {
            for (var i=0;i<3;i++)
            {
                var look = Rect(hero,"HeroLook"+i); Stretch(look);
                DrawHero(look,i);
                heroLooks[i] = look; heroWeapons[i] = sword;
                look.gameObject.SetActive(i==0);
            }
            sword = heroWeapons[0];
        }
        private void DrawHero(RectTransform root,int index)
        {
            if (index==0) BuildHero(root);
            else if (index==1) BuildWuxia(root);
            else BuildNinja(root);
        }
        private void SelectHero(int index)
        {
            if (simulation.TrySelectHero((HeroKind)index)) ApplyHeroVisual(index);
        }
        private void ApplyHeroVisual(int index)
        {
            SelectedHero=index;
            for (var i=0;i<3;i++) heroLooks[i].gameObject.SetActive(i==index);
            sword=heroWeapons[index];
            strike=0;
        }
        public void CycleHero()
        {
            if (IsChoosingHero || IsGrowthMenuOpen) return;
            CancelUpgradeHold();
            SelectHero((SelectedHero+1)%3);
        }
        public void ShowHeroSelection(Action<int> chosen)
        {
            if(selectionRoot!=null)Destroy(selectionRoot.gameObject);
            selectionRoot=Rect(contentRoot,"HeroSelection"); Stretch(selectionRoot);
            var background=Panel(selectionRoot,"SelectionBackdrop",Hex(0x102C2C)); Stretch(background.rectTransform);
            background.raycastTarget=true;
            Label(selectionRoot,"함께할 영웅을 선택하세요",46,Cream,.5f,.86f,1000,100);
            Label(selectionRoot,"한 명의 영웅과 함께하는 성장 모험",27,Hex(0xBED0B6),.5f,.80f,1000,65);
            var colors=new[]{Hex(0x32617A),Hex(0x4F8275),Hex(0x77517B)};
            var roles=new[]{"안정적인 단일 공격", "느리지만 강력한 강타", "빠른 이단 공격"};
            var previousWeapon=sword;
            for (var i=0;i<3;i++)
            {
                var index=i;
                var card=Panel(selectionRoot,"HeroCard"+i,Hex(0xE9E5D4));
                Anchor(card.rectTransform,.18f+i*.32f,.52f,310,680);
                var badge=Panel(card.rectTransform,"Banner",colors[i]);
                Anchor(badge.rectTransform,.5f,.98f,310,14);
                var art=Rect(card.rectTransform,"SelectionHero"+i); Anchor(art,.5f,.63f,220,300);
                art.localScale=Vector3.one*.9f;
                DrawHero(art,i);
                Label(card.rectTransform,HeroNames[i],25,Ink,.5f,.28f,300,70);
                Label(card.rectTransform,roles[i],19,Muted,.5f,.19f,300,50);
                var buttonLabel=MakeButton(card.rectTransform,"SelectHero"+i,.5f,.08f,270,80,colors[i],()=>
                {
                    if (!IsChoosingHero) return;
                    SelectHero(index);
                    selectionRoot.gameObject.SetActive(false);
                    chosen(index);
                },out _);
                buttonLabel.text="선택";
            }
            sword=previousWeapon;
            Label(selectionRoot,"직업마다 공격 주기와 타격 방식이 다릅니다.",25,Cream,.5f,.22f,1000,80);
            Label(selectionRoot,"선택한 영웅으로 이번 모험을 시작합니다.",22,Hex(0xA2BBA4),.5f,.16f,1000,70);
        }

        private void BuildWuxia(RectTransform root)
        {
            OvalAt(root,"Shadow",new Color(0,0,0,.18f),0,-136,205,35);
            OvalAt(root,"LongHair",Hex(0x283B43),-43,47,68,177);
            Box(root,"BackBoot",Hex(0x3C4D51),-30,-115,32,53);
            Box(root,"FrontBoot",Hex(0x3C4D51),31,-115,34,53);
            OvalAt(root,"Robe",Hex(0xEAE8D2),0,-35,119,145);
            var left=Box(root,"RobeLeft",Hex(0xCCDDCF),-28,-50,47,121);
            left.rectTransform.localRotation=Quaternion.Euler(0,0,-9);
            var right=Box(root,"RobeRight",Hex(0xF5F0DF),28,-46,48,122);
            right.rectTransform.localRotation=Quaternion.Euler(0,0,9);
            Box(root,"Sash",Hex(0x478B80),2,-26,115,20);
            var ribbon=Box(root,"SashTail",Hex(0x478B80),-48,-78,16,87);
            ribbon.rectTransform.localRotation=Quaternion.Euler(0,0,-16);
            OvalAt(root,"Sleeve",Hex(0xD9E3D5),-57,-4,50,73);
            OvalAt(root,"Face",Hex(0xEAC396),10,72,91,100);
            OvalAt(root,"Hair",Hex(0x283B43),-1,105,104,70);
            var fringe=Box(root,"Fringe",Hex(0x283B43),-27,83,24,63);
            fringe.rectTransform.localRotation=Quaternion.Euler(0,0,12);
            OvalAt(root,"Topknot",Hex(0x283B43),-21,149,42,42);
            Box(root,"HairTie",Hex(0x7AB7A0),-21,132,42,10);
            Box(root,"Eye",Ink,43,72,10,15);
            Box(root,"Collar",Hex(0x478B80),9,24,66,15);
            sword=Rect(root,"JianArm"); Anchor(sword,.5f,.5f,160,220);
            sword.anchoredPosition=new Vector2(59,-15);
            OvalAt(sword,"Hand",Hex(0xEAC396),5,-9,34,34);
            Box(sword,"Grip",Hex(0x478B80),18,1,14,65);
            Box(sword,"Guard",Hex(0xD3B66E),18,23,58,12);
            Box(sword,"Jian",Hex(0xD5ECE7),18,113,20,173);
            Box(sword,"Edge",Cream,23,114,6,172);
            var tassel=Box(sword,"Tassel",Hex(0x6EAF9B),17,-53,10,57);
            tassel.rectTransform.localRotation=Quaternion.Euler(0,0,-18);
            sword.localRotation=Quaternion.Euler(0,0,-18);
        }

        private void BuildNinja(RectTransform root)
        {
            OvalAt(root,"Shadow",new Color(0,0,0,.18f),0,-136,205,35);
            var pony=OvalAt(root,"Ponytail",Hex(0x302D46),-70,55,60,152);
            pony.rectTransform.localRotation=Quaternion.Euler(0,0,-24);
            OvalAt(root,"HairTie",Hex(0xCE84A1),-55,108,28,27);
            Box(root,"BackBoot",Hex(0x33364C),-29,-112,31,65);
            Box(root,"FrontBoot",Hex(0x33364C),31,-112,33,65);
            Box(root,"LegWrap",Hex(0xB6A4C4),31,-100,35,12);
            OvalAt(root,"Tunic",Hex(0x725C8A),0,-36,101,124);
            Box(root,"WaistSash",Hex(0xCA7B9A),0,-42,106,17);
            OvalAt(root,"Shoulder",Hex(0x8C729E),-45,2,42,50);
            OvalAt(root,"Face",Hex(0xEAC396),10,72,91,100);
            OvalAt(root,"Hair",Hex(0x302D46),-4,105,110,74);
            var fringe=Box(root,"Fringe",Hex(0x302D46),-28,86,35,57);
            fringe.rectTransform.localRotation=Quaternion.Euler(0,0,18);
            Box(root,"Headband",Hex(0x725C8A),9,96,101,18);
            Box(root,"Eye",Ink,43,76,10,16);
            var lash=Box(root,"Lash",Ink,47,85,16,5);
            lash.rectTransform.localRotation=Quaternion.Euler(0,0,-20);
            Box(root,"Mask",Hex(0x4B435F),13,43,84,38);
            Box(root,"Scarf",Hex(0xAA7095),-3,21,111,23);
            var scarf=Box(root,"ScarfTail",Hex(0xAA7095),-86,24,107,22);
            scarf.rectTransform.localRotation=Quaternion.Euler(0,0,-15);
            var backKunai=Box(root,"BackKunai",Hex(0xB7CED2),-63,-23,13,82);
            backKunai.rectTransform.localRotation=Quaternion.Euler(0,0,-37);
            sword=Rect(root,"KunaiArm"); Anchor(sword,.5f,.5f,160,220);
            sword.anchoredPosition=new Vector2(58,-15);
            OvalAt(sword,"Hand",Hex(0xEAC396),5,-9,32,32);
            Box(sword,"Grip",Hex(0x463C59),18,2,17,59);
            Box(sword,"Guard",Hex(0xB9ACCA),18,23,43,10);
            var blade=Box(sword,"Kunai",Hex(0xD7E9E4),18,69,29,75);
            blade.rectTransform.localRotation=Quaternion.Euler(0,0,0);
            OvalAt(sword,"Pommel",Hex(0xB9ACCA),18,-29,22,22);
            sword.localRotation=Quaternion.Euler(0,0,-18);
        }

        private void BuildMonsterLooks()
        {
            var golem=Rect(monster,"GolemArt"); Stretch(golem); golemBody=BuildGolem(golem);
            var orc=Rect(monster,"OrcArt"); Stretch(orc); orcBody=BuildOrc(orc);
            var goblin=Rect(monster,"GoblinArt"); Stretch(goblin); goblinBody=BuildGoblin(goblin);
            var king=Rect(monster,"SlimeKingArt"); Stretch(king); BuildSlime(king);
            Box(king,"CrownBand",Gold,0,125,140,28);
            for(var i=0;i<3;i++) { var tip=Box(king,"CrownPoint",Gold,-52+i*52,154,32,48); tip.rectTransform.localRotation=Quaternion.Euler(0,0,i==0?15:i==2?-15:0); }
            OvalAt(king,"RoyalGem",Hex(0xC36348),0,126,23,26);
            var chief=Rect(monster,"GoblinChiefArt"); Stretch(chief); BuildGoblin(chief);
            Box(chief,"Headband",Hex(0xA84F49),0,130,172,23);
            var feather=OvalAt(chief,"Feather",Gold,65,175,33,110); feather.rectTransform.localRotation=Quaternion.Euler(0,0,-28);
            OvalAt(chief,"GoldMedallion",Gold,0,-12,52,52);
            Box(chief,"BannerPole",Hex(0x9F784C),135,24,12,270);
            Box(chief,"WarBanner",Hex(0xA84F49),173,103,88,70);
            var warlord=Rect(monster,"OrcChiefArt"); Stretch(warlord); BuildOrc(warlord);
            Box(warlord,"WarHelm",Hex(0x6D5556),0,142,162,45);
            for(var i=0;i<2;i++) { var horn=Box(warlord,"IvoryHorn",Cream,-70+i*140,177,27,77); horn.rectTransform.localRotation=Quaternion.Euler(0,0,i==0?27:-27); }
            OvalAt(warlord,"ChestPlate",Hex(0xA86247),0,-5,112,77);
            Box(warlord,"ChiefEmblem",Gold,0,-6,28,43);
            var ancient=Rect(monster,"AncientGolemArt"); Stretch(ancient); BuildGolem(ancient);
            for(var i=0;i<3;i++) { var crystal=Box(ancient,"AncientCrystal",Hex(0xAFA3E6),-105+i*105,152+(i==1?40:0),32,95); crystal.rectTransform.localRotation=Quaternion.Euler(0,0,35); }
            OvalAt(ancient,"AncientRune",Hex(0xC4B1EE),0,-26,76,76);
            Box(ancient,"RuneCore",Cream,0,-26,14,42);
            enemyLooks=new[]{slime,golem,orc,goblin,guardian,king,chief,warlord,ancient};
            ancientCracks=new[]{(RectTransform)ancient.Find("Crack"),(RectTransform)ancient.Find("CrackTwo")};
            golemCracks=new[]{(RectTransform)golem.Find("Crack"),(RectTransform)golem.Find("CrackTwo")};
            BuildExtraMonsters();
        }
        private Image BuildGolem(RectTransform root)
        {
            OvalAt(root,"Shadow",new Color(0,0,0,.18f),0,-133,340,45);
            OvalAt(root,"BackFoot",Hex(0x728587),-69,-104,90,67);
            OvalAt(root,"FrontFoot",Hex(0x728587),70,-104,90,67);
            OvalAt(root,"LeftBoulder",Hex(0x819698),-130,-18,95,140);
            OvalAt(root,"RightBoulder",Hex(0x819698),130,-18,95,140);
            var body=OvalAt(root,"GolemBody",Hex(0x98A9AA),0,-18,244,220);
            var head=Box(root,"StoneHead",Hex(0xB1BEBA),0,89,158,110);
            Box(head.rectTransform,"Brow",Hex(0x728587),0,12,137,21);
            for(var i=0;i<2;i++) Box(root,"CrystalEye",Hex(0x9DE1DB),-41+i*80,87,23,21);
            Box(root,"Mouth",Hex(0x607879),0,43,49,9);
            var core=Box(root,"CrystalCore",Hex(0x9DE1DB),0,-31,36,50);
            core.rectTransform.localRotation=Quaternion.Euler(0,0,45);
            OvalAt(root,"Moss",Hex(0x7CA572),-51,150,101,22);
            Box(root,"Crack",Hex(0x728587),67,-37,7,62);
            var crack=Box(root,"CrackTwo",Hex(0x607879),-57,-13,7,68);
            crack.rectTransform.localRotation=Quaternion.Euler(0,0,-28);
            return body;
        }
        private Image BuildOrc(RectTransform root)
        {
            OvalAt(root,"Shadow",new Color(0,0,0,.18f),0,-133,340,45);
            Box(root,"LeftBoot",Hex(0x66523E),-57,-104,63,64);
            Box(root,"RightBoot",Hex(0x66523E),60,-104,63,64);
            OvalAt(root,"LeftArm",Hex(0x75965E),-111,-12,66,119);
            OvalAt(root,"RightArm",Hex(0x75965E),112,-12,66,119);
            var body=OvalAt(root,"OrcBody",Hex(0x81A56B),0,-21,211,208);
            Box(root,"Belt",Hex(0x77553D),0,-65,204,31);
            Box(root,"Buckle",Hex(0xD4B16A),0,-65,34,34);
            OvalAt(root,"Face",Hex(0x98B780),0,77,158,132);
            OvalAt(root,"Ear",Hex(0x81A56B),-86,92,66,35);
            OvalAt(root,"Ear",Hex(0x81A56B),86,92,66,35);
            Box(root,"Hair",Hex(0x4B5040),0,143,78,30);
            for(var i=0;i<2;i++)
            {
                Box(root,"Eye",Ink,-36+i*72,91,11,19);
                Box(root,"Tusk",Cream,-43+i*86,28,17,33);
            }
            Box(root,"Mouth",Hex(0x60734B),0,35,63,12);
            OvalAt(root,"ShoulderPad",Hex(0xA28D65),-92,18,75,62);
            var axe=Rect(root,"Axe"); Anchor(axe,.5f,.5f,100,200); axe.anchoredPosition=new Vector2(-151,17);
            Box(axe,"Handle",Hex(0x9F784C),0,-3,15,181);
            OvalAt(axe,"AxeBlade",Hex(0xC1CCBE),-15,55,103,79);
            axe.localRotation=Quaternion.Euler(0,0,-13);
            return body;
        }
        private Image BuildGoblin(RectTransform root)
        {
            OvalAt(root,"Shadow",new Color(0,0,0,.18f),0,-133,300,43);
            Box(root,"Boot",Hex(0x66513D),-43,-110,49,55);
            Box(root,"Boot",Hex(0x66513D),46,-110,49,55);
            var body=OvalAt(root,"GoblinBody",Hex(0x9EBE77),0,-30,153,170);
            OvalAt(root,"Vest",Hex(0x8B6847),0,-35,135,124);
            Box(root,"Belt",Hex(0x5A4A39),0,-66,138,17);
            var ear=OvalAt(root,"LeftEar",Hex(0x88AC65),-103,88,113,49);
            ear.rectTransform.localRotation=Quaternion.Euler(0,0,-22);
            ear=OvalAt(root,"RightEar",Hex(0x88AC65),103,88,113,49);
            ear.rectTransform.localRotation=Quaternion.Euler(0,0,22);
            OvalAt(root,"Head",Hex(0xA9C784),0,76,173,141);
            Box(root,"Hair",Hex(0x5C713D),-12,153,61,30);
            for(var i=0;i<2;i++)
            {
                OvalAt(root,"Eye",Hex(0xE8D48A),-42+i*84,88,37,41);
                Box(root,"Pupil",Ink,-48+i*84,88,9,22);
            }
            OvalAt(root,"Nose",Hex(0x88AC65),-2,62,34,35);
            Box(root,"Grin",Hex(0x62753E),3,31,72,14);
            Box(root,"Tooth",Cream,28,28,15,18);
            OvalAt(root,"Arm",Hex(0x9EBE77),-88,-9,46,90);
            OvalAt(root,"Arm",Hex(0x9EBE77),86,-9,46,90);
            var knife=Box(root,"Dagger",Hex(0xD5DECF),-117,8,20,101);
            knife.rectTransform.localRotation=Quaternion.Euler(0,0,-25);
            Box(root,"DaggerGrip",Hex(0x655039),-98,-46,15,37);
            OvalAt(root,"Pouch",Hex(0xB39662),67,-59,50,62);
            return body;
        }

        /// <summary>A contact sheet built from the exact runtime art, for opt-in development capture.</summary>
        public GameObject CreateArtGallery()
        {
            var root=Rect(contentRoot,"ArtGallery"); Stretch(root);
            Stretch(Panel(root,"GalleryBackground",Hex(0x173A36)).rectTransform);
            Label(root,"리프트레일 · 작은 모험가들",40,Cream,.5f,.945f,1040,90);
            Label(root,"영웅과 몬스터 모음",24,Hex(0xBED0B6),.5f,.905f,1040,60);
            var previousWeapon=sword;
            for(var i=0;i<3;i++)
            {
                var art=Rect(root,"GalleryHero"+i); Anchor(art,.5f,.5f,220,300);
                art.anchoredPosition=new Vector2(-350+i*350,450);
                DrawHero(art,i);
                Label(root,HeroNames[i],27,Cream,.176f+i*.324f,.625f,340,80);
            }
            sword=previousWeapon;
            var names=new[]{"숲 슬라임","꼬마 골렘","숲 오크","이끼 고블린"};
            for(var i=0;i<4;i++)
            {
                var art=Rect(root,"GalleryMonster"+i); Anchor(art,.5f,.5f,360,350);
                art.anchoredPosition=new Vector2(i%2==0?-260:260,i<2?-100:-560);
                art.localScale=Vector3.one*.86f;
                if(i==0) BuildSlime(art); else if(i==1) BuildGolem(art); else if(i==2) BuildOrc(art); else BuildGoblin(art);
                Label(root,names[i],27,Cream,i%2==0?.26f:.74f,i<2?.33f:.09f,500,70);
            }
            Label(root,"동글동글한 모습, 저마다 다른 개성",22,Hex(0xBED0B6),.5f,.025f,1040,60);
            return root.gameObject;
        }
    }
}
