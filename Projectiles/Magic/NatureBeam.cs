using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.Enums;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;

namespace Antiaris.Projectiles.Magic
{
	public class NatureBeam : ModProjectile
	{
	    private const float maxchargeValue = 50f;
	    private const float movedistance = 60f;

	    private int manaCheck = 0;

	    public float distance
		{
			get { return projectile.ai[0]; }
			set { projectile.ai[0] = value; }
		}

	    public float charge
		{
			get { return projectile.localAI[0]; }
			set { projectile.localAI[0] = value; }
		}

	    public bool atMaxcharge { get { return this.charge == maxchargeValue; } }
	    public override Color? GetAlpha(Color lightColor) { return Color.White; }

	    public override void SetDefaults()
		{
			projectile.width = 10;
			projectile.height = 10;
			projectile.friendly = true;
			projectile.penetrate = -1;
			projectile.tileCollide = false;
			projectile.magic = true;
			projectile.hide = true;
        }

	    public override void SetStaticDefaults()
        {
            DisplayName.SetDefault("Nature Beam");
			DisplayName.AddTranslation(GameCulture.Chinese, "自然光线");
            DisplayName.AddTranslation(GameCulture.Russian, "Природный луч");
        }

	    public override bool PreDraw(SpriteBatch spriteBatch, Color lightColor)
		{
			if (this.atMaxcharge)
			{
				this.drawLaser(spriteBatch, Main.projectileTexture[projectile.type], Main.player[projectile.owner].Center,
					projectile.velocity, 10, projectile.damage, -1.57f, 1f, 1000f, Color.White, (int)movedistance);
			}
			return false;
		}

	    public void drawLaser(SpriteBatch spriteBatch, Texture2D texture, Vector2 start, Vector2 unit, float step, int damage, float rotation = 0f, float scale = 1f, float maxDist = 2000f, Color color = default(Color), int transDist = 50)
		{
			Vector2 origin = start;
			float r = unit.ToRotation() + rotation;
			for (float i = transDist; i <= this.distance; i += step)
			{
				Color c = Color.White;
				origin = start + i * unit;
				spriteBatch.Draw(texture, origin - Main.screenPosition,
					new Rectangle(0, 26, 24, 26), i < transDist ? Color.Transparent : c, r,
					new Vector2(24 * .5f, 26 * .5f), scale, 0, 0);
			}
			spriteBatch.Draw(texture, start + unit * (transDist - step) - Main.screenPosition,
				new Rectangle(0, 0, 24, 26), Color.White, r, new Vector2(24 * .5f, 26 * .5f), scale, 0, 0);
			spriteBatch.Draw(texture, start + (this.distance + step) * unit - Main.screenPosition,
				new Rectangle(0, 52, 24, 26), Color.White, r, new Vector2(24 * .5f, 26 * .5f), scale, 0, 0);
		}

	    public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox)
		{
			if (this.atMaxcharge)
			{
				Player player = Main.player[projectile.owner];
				Vector2 unit = projectile.velocity;
				float point = 0f;
				return Collision.CheckAABBvLineCollision(targetHitbox.TopLeft(), targetHitbox.Size(), player.Center,
					player.Center + unit * this.distance, 22, ref point);
			}
			return false;
		}

	    public override void OnHitNPC(NPC target, int damage, float knockback, bool crit)
		{
			target.immune[projectile.owner] = 10;
			target.AddBuff(BuffID.Poisoned, 220, false);
		}

	    public override void AI()
		{
			Vector2 mousePos = Main.MouseWorld;
			Player player = Main.player[projectile.owner];
			if (projectile.owner == Main.myPlayer)
			{
				Vector2 diff = mousePos - player.Center;
				diff.Normalize();
				projectile.velocity = diff;
				projectile.direction = Main.MouseWorld.X > player.position.X ? 1 : -1;
				projectile.netUpdate = true;
			}
			projectile.position = player.Center + projectile.velocity * movedistance;
			projectile.timeLeft = 2;
			int dir = projectile.direction;
			player.ChangeDir(dir);
			player.heldProj = projectile.whoAmI;
			player.itemTime = 2;
			player.itemAnimation = 2;
			player.itemRotation = (float)Math.Atan2(projectile.velocity.Y * dir, projectile.velocity.X * dir);
			int delay = 10;
			++projectile.ai[0];
			if ((double)projectile.ai[0] >= 1.0) projectile.ai[0] = 0.0f;
			if (projectile.soundDelay <= 0 && player.channel && this.charge >= 40.0f)
            {
                projectile.soundDelay = delay;
                projectile.soundDelay *= 2;
                if ((double)projectile.ai[0] != 1.0)
                    Main.PlaySound(2, (int)projectile.position.X, (int)projectile.position.Y, 15);
            }
			if (!player.channel || (double)player.statMana <= (double)player.inventory[player.selectedItem].mana)
				projectile.Kill();
			else
			{
                ++this.manaCheck;
				if (this.manaCheck % 10 < 1 && !player.CheckMana(player.inventory[player.selectedItem].mana, true) && this.charge < maxchargeValue) projectile.Kill();
				Vector2 offset = projectile.velocity;
				offset *= movedistance - 15;
				Vector2 pos = player.Center + offset - new Vector2(13, 10);
                if (this.charge < maxchargeValue)
                {
                    ++this.charge;
                    int chargeFact = (int)(this.charge / 20f);
                    Vector2 dustVelocity = Vector2.UnitX * 18f;
                    dustVelocity = dustVelocity.RotatedBy(projectile.rotation - 1.57f, default(Vector2));
                    Vector2 spawnPos = projectile.Center + dustVelocity;
                    for (int k = 0; k < chargeFact + 1; k++)
                    {
                        Vector2 spawn = spawnPos + ((float)Main.rand.NextDouble() * 6.28f).ToRotationVector2() * (12f - (chargeFact * 2));
                        Dust dust = Main.dust[Dust.NewDust(pos, 20, 20, 61, projectile.velocity.X / 2f,
                            projectile.velocity.Y / 2f, 0, default(Color), 1f)];
                        dust.velocity = Vector2.Normalize(spawnPos - spawn) * 1.5f * (10f - chargeFact * 2f) / 10f;
                        dust.noGravity = true;
                        dust.scale = Main.rand.Next(10, 20) * 0.05f;
                    }
                }
            }
			if (this.charge < maxchargeValue) return;
			Vector2 start = player.Center;
			Vector2 unit = projectile.velocity;
			unit *= -1;
			for (this.distance = movedistance; this.distance <= 2200f; this.distance += 5f)
			{
				start = player.Center + projectile.velocity * this.distance;
				if (!Collision.CanHit(player.Center, 1, 1, start, 1, 1))
				{
					this.distance -= 5f;
					break;
				}
			}
        }

	    public override bool ShouldUpdatePosition()
		{
			return false;
		}

	    public override void CutTiles()
		{
			DelegateMethods.tilecut_0 = TileCuttingContext.AttackProjectile;
			Vector2 unit = projectile.velocity;
			Utils.PlotTileLine(projectile.Center, projectile.Center + unit * distance, (projectile.width + 16) * projectile.scale, DelegateMethods.CutTiles);
		}
	}
}
