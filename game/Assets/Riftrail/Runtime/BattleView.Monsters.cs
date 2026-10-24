using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
namespace Riftrail
{
    public sealed partial class BattleView
    {
        private readonly List<Image> creatureBodies=new List<Image>();
        private RectTransform sleepyMark;
        private Image iceShell;
        private void BuildExtraMonsters()
        {
            var looks=new List<RectTransform>(enemyLooks);
            for(var i=0;i<12;i++)
            {
                var family=i%6; var boss=i>=6;
                var root=Rect(monster,((MonsterKind)(9+i))+"Art"); Stretch(root);
                OvalAt(root,"Shadow",new Color(0,0,0,.18f),0,-133,300,40);
                var body=DrawCreature(root,family,boss); creatureBodies.Add(body); looks.Add(root);
            }
            enemyLooks=looks.ToArray();
            iceShell=OvalAt(monster,"IceShield",new Color(.55f,.88f,1,.18f),0,5,330,360);
            sleepyMark=Label(monster,"쿨쿨",36,Gold,.5f,.5f,180,65).rectTransform;
            sleepyMark.anchoredPosition=new Vector2(65,170);
        }
        private Image DrawCreature(RectTransform r,int family,bool boss)
        {
            Image body;
            if(family==0)
            {
                OvalAt(r,"Foot",Hex(0xD8C59A),-45,-114,63,42); OvalAt(r,"Foot",Hex(0xD8C59A),45,-114,63,42);
                body=OvalAt(r,"Stem",Cream,0,-42,138,172);
                OvalAt(r,"MushroomCap",Hex(boss?0x9470A9:0xC96D62),0,81,302,146);
                for(var j=0;j<5;j++) OvalAt(r,"CapSpot",Cream,-105+j*52,86+(j%2)*32,35,25);
                Eyes(r,0,-27,36); Box(r,"Mouth",Ink,0,-62,31,7);
            }
            else if(family==1)
            {
                Box(r,"BoneLeg",Cream,-39,-90,17,86); Box(r,"BoneLeg",Cream,39,-90,17,86);
                Box(r,"Boot",Hex(0x52646D),-40,-126,48,25); Box(r,"Boot",Hex(0x52646D),40,-126,48,25);
                body=OvalAt(r,"Ribs",Hex(0xD7DBCB),0,-31,107,122);
                for(var j=0;j<3;j++) Box(r,"RibLine",Hex(0x52646D),0,-10-j*27,83,9);
                OvalAt(r,"Skull",Cream,0,82,188,157); Eyes(r,0,95,44);
                Box(r,"Nose",Hex(0x52646D),0,65,18,20); Box(r,"Jaw",Cream,0,13,106,38);
                for(var j=0;j<3;j++) Box(r,"ToothGap",Hex(0x7F8A82),-28+j*28,14,6,23);
                Box(r,"BoneArm",Cream,-80,-20,18,100); Box(r,"BoneArm",Cream,80,-20,18,100);
                if(boss) { Box(r,"Helm",Hex(0x788A9B),0,149,201,43); Box(r,"Visor",Hex(0x788A9B),0,82,20,140);
                    Box(r,"Sword",Hex(0xCDDDE0),115,2,20,150); OvalAt(r,"Shield",Hex(0x7C6A97),-109,-31,78,100); }
            }
            else if(family==2)
            {
                body=OvalAt(r,"DokebiBody",Hex(boss?0xB67977:0x8FA8B1),0,-33,183,184);
                Box(r,"TigerBelt",Gold,0,-78,175,29);
                OvalAt(r,"Face",Hex(boss?0xD99485:0xABC3C4),0,83,171,145); Eyes(r,0,85,35);
                Box(r,"Horn",Cream,0,170,29,65); Box(r,"Grin",Ink,0,42,66,13);
                Box(r,"Fang",Cream,-23,37,13,27); Box(r,"Fang",Cream,23,37,13,27);
                Box(r,"ClubGrip",Hex(0x8C694D),-129,-4,19,162);
                OvalAt(r,"HugeClub",Hex(0xA28560),-129,88,83,150);
                for(var j=0;j<3;j++) Box(r,"ClubStud",Cream,-157+j*26,91+(j%2)*30,15,19);
                OvalAt(r,"MoneySack",Gold,115,-71,89,96); Box(r,"SackTie",Hex(0x95733D),115,-21,49,12);
            }
            else if(family==3)
            {
                body=OvalAt(r,"SnowBody",Hex(0xC9E6E8),0,-28,235,214);
                OvalAt(r,"SnowHead",Cream,0,81,167,143); Eyes(r,0,93,38);
                OvalAt(r,"SnowFoot",Cream,-68,-123,94,43); OvalAt(r,"SnowFoot",Cream,68,-123,94,43);
                Box(r,"Scarf",Hex(0x6A93B8),0,22,180,27); Box(r,"ScarfTail",Hex(0x6A93B8),75,-9,25,79);
                if(boss) { OvalAt(r,"YetiArm",Cream,-137,-13,74,160); OvalAt(r,"YetiArm",Cream,137,-13,74,160);
                    for(var j=0;j<2;j++){var horn=Box(r,"IceHorn",Hex(0xAFA3E6),-66+j*132,158,27,90);horn.rectTransform.localRotation=Quaternion.Euler(0,0,j==0?25:-25);} }
            }
            else if(family==4)
            {
                body=OvalAt(r,"GhostBody",Hex(boss?0xD8CCED:0xDBE9D8),0,6,211,271);
                for(var j=0;j<4;j++) OvalAt(r,"GhostHem",Hex(boss?0xD8CCED:0xDBE9D8),-74+j*49,-110,56,54);
                OvalAt(r,"GhostArm",Cream,-117,11,68,78); OvalAt(r,"GhostArm",Cream,117,11,68,78);
                Eyes(r,0,53,41); OvalAt(r,"GhostMouth",Ink,0,8,33,40);
                if(boss) { Box(r,"TopHat",Hex(0x45475F),0,160,128,93); Box(r,"HatBrim",Hex(0x45475F),0,117,188,22);
                    Box(r,"HatBand",Gold,0,133,127,18); OvalAt(r,"Monocle",Gold,41,55,56,56); OvalAt(r,"Lens",Cream,41,55,43,43);
                    Box(r,"BowTie",Hex(0xAF6A8D),0,-27,94,26); }
            }
            else
            {
                var color=Hex(boss?0xD7B566:0x88B49B);
                for(var j=0;j<2;j++) {var wing=OvalAt(r,"Wing",Hex(boss?0xBA855B:0x6F8EAC),-132+j*264,64,137,152);wing.rectTransform.localRotation=Quaternion.Euler(0,0,j==0?-35:35);}
                var tail=OvalAt(r,"Tail",color,109,-81,175,53); tail.rectTransform.localRotation=Quaternion.Euler(0,0,25);
                body=OvalAt(r,"DragonBody",color,0,-29,193,208); OvalAt(r,"Belly",Cream,0,-47,115,126);
                OvalAt(r,"DragonHead",color,0,92,186,140); OvalAt(r,"Snout",color,47,59,126,70);
                Eyes(r,-5,101,42); Box(r,"Fang",Cream,76,26,14,29);
                for(var j=0;j<2;j++) {var horn=Box(r,"Horn",Cream,-58+j*116,166,21,70);horn.rectTransform.localRotation=Quaternion.Euler(0,0,j==0?25:-25);}
                if(boss) for(var j=0;j<3;j++){var gem=Box(r,"TreasureGem",Hex(0xAF83C4),-63+j*64,-137,36,36);gem.rectTransform.localRotation=Quaternion.Euler(0,0,45);}
            }
            if(boss && (family==0||family==2)) { Box(r,"Crown",Gold,0,181,107,25);
                for(var j=0;j<3;j++) Box(r,"CrownPoint",Gold,-39+j*39,205,22,42); }
            return body;
        }
        private void Eyes(RectTransform root,float x,float y,float spread)
        {
            for(var j=0;j<2;j++) {OvalAt(root,"Eye",Ink,x-spread+j*spread*2,y,17,25); OvalAt(root,"EyeLight",Cream,x-spread-3+j*spread*2,y+6,5,7);}
        }
        private void RenderExtraMonsters(BattleSimulation s,float impact)
        {
            sleepyMark.gameObject.SetActive(s.Enemy.IsSleeping);
            iceShell.gameObject.SetActive(s.Enemy.Shield>0);
            if(!s.Enemy.IsExposed) monsterAlpha.alpha=.25f;
            if(StageCatalog.IsSkeleton(s.Enemy.Kind) && s.Enemy.Revived) monster.localScale*=.87f;
            if((int)s.Enemy.Kind>=9)
            {
                var i=(int)s.Enemy.Kind-9; var body=creatureBodies[i];
                var tint=StageCatalog.IsDragon(s.Enemy.Kind) && s.Enemy.IsArmored ? Hex(0xB08A77) : Hex(i%6==0?0xF5F0DF:i%6==1?0xD7DBCB:i%6==2?(i>=6?0xB67977:0x8FA8B1):i%6==3?0xC9E6E8:i%6==4?(i>=6?0xD8CCED:0xDBE9D8):(i>=6?0xD7B566:0x88B49B));
                body.color=Color.Lerp(tint,Cream,impact*.8f);
            }
        }
    }
}
