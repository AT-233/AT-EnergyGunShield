using System;
using System.IO;
using System.Reflection;
using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using HarmonyLib;
using UnityEngine;
using Object = UnityEngine.Object;

namespace EnergyGunShield
{
    [BepInPlugin("EnergyGunShield", "AT.能量枪盾EnergyGunShield", "1.0.0")]
    public class EnergyGunShieldCore : BaseUnityPlugin
    {
        private static ManualLogSource logger;
        public static ConfigEntry<int> ShieldEnergy;
        public static ConfigEntry<float> ShieldCoolTime;
        public static ConfigEntry<float> ShieldTime;
        public static ConfigEntry<KeyCode> OPEN;

        public void Awake()
        {
            var harmony = new HarmonyLib.Harmony("EnergyGunShield");
            harmony.PatchAll(Assembly.GetExecutingAssembly());
            logger = Logger;
            Logger.LogInfo($"nanosuit: Loading");
            ShieldEnergy = Config.Bind<int>("能量枪盾配置(EnergyGunShield Settings)", "最大能量值(Max Energy)", 1000, "持续时间内受到伤害总数超过这个之，枪盾会消失(If the total number of damage sustained exceeds this, the gun shield will disappear)");
            ShieldCoolTime = Config.Bind<float>("能量枪盾配置(EnergyGunShield Settings)", "充能时间(Charging time)", 10, "枪盾能量耗尽或者持续时间结束后，再次开启的时间设置(Set the time for shield to turn on again after it runs out of power or the duration ends)");
            ShieldTime = Config.Bind<float>("能量枪盾配置(EnergyGunShield Settings)", "持续时间(Charging time)", 6, "枪盾开启后的持续时间，时间到会消失(The duration after the gun shield is activated. Time will disappear)");
            OPEN = Config.Bind<KeyCode>("能量枪盾配置(EnergyGunShield Settings)", "按键设置(KeyCode Settings)", KeyCode.Mouse2, "枪盾启动按钮(start button)");
        }
        [HarmonyPatch(typeof(GClass2623), "HitCollider", MethodType.Getter)]
        public class Shield_Damage_Patch
        {

            public static void Postfix(GClass2623 __instance, ref Collider __result)//350是2611,351-353是2620, 355是2623
            {
                var ammo = __instance.Ammo as BulletClass;
                if (__result != null)
                {
                    var shieldScript = __result.GetComponentInParent<gunshielddestroy>();
                    if (shieldScript != null)
                    {
                        shieldScript.ReduceShieldDamage(ammo.Damage);
                    }
                }
            }
        }
    }
    public class EnergyGunShield : MonoBehaviour
    {
        float timer = 0;
        bool isStartTimer = false;
        bool isskill = false;
        private MeshRenderer baseRender;
        public static Object GunShieldPrefab { get; private set; }


        void Awake()
        {

        }
        void Start()
        {
            baseRender = this.GetComponent<MeshRenderer>();
            baseRender.material.color = Color.green;
            isskill = true;
            if (GunShieldPrefab == null)
            {
                String filename = Path.Combine(Environment.CurrentDirectory, "BepInEx/plugins/atmod/energygunshield");
                if (!File.Exists(filename))
                    return;

                var shieldBundle = AssetBundle.LoadFromFile(filename);
                if (shieldBundle == null)
                    return;
                GunShieldPrefab = shieldBundle.LoadAsset("Assets/Energygunshield/Energygunshield.prefab");
                Console.WriteLine("已加载枪盾");
            }

        }

        // Update is called once per frame
        void Update()
        {
            if (Input.GetKeyDown(EnergyGunShieldCore.OPEN.Value) && isskill)
            {
                isskill = false;
                var wall = Instantiate(GunShieldPrefab, this.transform.position, this.transform.rotation);
                var wallshield = wall as GameObject;
                wallshield.transform.parent = this.transform;
                isStartTimer = true;
                baseRender.material.color = Color.red;
            }

            if (isStartTimer)
            {
                timer += Time.deltaTime;
                if (timer >= EnergyGunShieldCore.ShieldCoolTime.Value)
                {
                    baseRender.material.color = Color.green;
                    isStartTimer = false;
                    isskill = true;
                    timer = 0;
                }
            }
        }
    }
    public class gunshielddestroy : MonoBehaviour
    {
        float timer = 0;
        bool isStartTimer = false;
        private int shieldhealth;
        public GameObject shieldobject;
        [ColorUsageAttribute(true, true)]
        public Color halfcol;
        [ColorUsageAttribute(true, true)]
        public Color dangercol;
        private float halfhealth;
        private float tophealth;
        private float dangerhealth;

        void Start()
        {
            shieldhealth = EnergyGunShieldCore.ShieldEnergy.Value;
            isStartTimer = true;
            tophealth = shieldhealth;
            halfhealth = tophealth / 2;
            dangerhealth = tophealth / 4;
        }

        // Update is called once per frame
        void Update()
        {
            if (isStartTimer)
            {
                timer += Time.deltaTime;
                if (timer >= EnergyGunShieldCore.ShieldTime.Value)
                {
                    Destroy(this.gameObject);
                    isStartTimer = false;
                    timer = 0;
                }
            }
        }
        public void ReduceShieldDamage(int damage)
        {     
            shieldhealth -= damage;           
            if (shieldhealth <= halfhealth && shieldhealth > dangerhealth)
            {
                shieldobject.GetComponent<MeshRenderer>().materials[0].SetColor("_Edgecolor", halfcol);
            }
            if (shieldhealth <= dangerhealth)
            {
                shieldobject.GetComponent<MeshRenderer>().materials[0].SetColor("_Edgecolor", dangercol);
            }
            if (shieldhealth <= 0)
            {
                Destroy(this.gameObject);
            }
        }
    }
    public class armorhide : MonoBehaviour
    {
        void FixedUpdate()
        {
            if (Input.GetMouseButtonDown(0))//点击时取消碰撞体积
            {
                this.GetComponent<MeshCollider>().enabled = false;
            }
            if (Input.GetMouseButtonUp(0))//抬起时显示碰撞体积
            {
                this.GetComponent<MeshCollider>().enabled = true;
            }

        }
    }
}
