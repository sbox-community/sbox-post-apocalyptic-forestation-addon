

// Further possible enhancements;
//
// Particle birds, forest ambients, soundscapes and sunrays
// water surfaces can have moss
// Textures of the map can be modified
// Roof detecting
// Enhancement of map compatibility

using Sandbox;
using System.Collections.Generic;
using System.Linq;
using static Sandbox.Event.Entity;
using System.Threading.Tasks;
using System.IO;
using System;

public partial class PostApocalypticForestation
{
	private static readonly bool _debug = false;

	static List<ModelEntity> spawnedTrees = null; //svside
	static List<ModelEntity> spawnedRocks = null; //svside
	static List<ModelEntity> spawnedGrasses = null; //clside
	static List<ModelEntity> spawnedIvys = null; //clside
	static List<ModelEntity> spawnedDebris = null; //clside

	static List<EntInfo> doneGrasses = new();
	static List<EntInfo> doneIvys = new();
	static List<EntInfo> doneDebris = new();

	static GradientFogController? defaultFog;
	static Color? defaultAmbientColor;

	static Dictionary<long, Client> spamProtect = new();
	static bool joinedCL = false;

	// Convars saving not working?
	[ConVar.Replicated( "sv_paf_autogenerate", Help = "Auto generation of post apocalyptic forestation", Saved = true )]
	public static bool autoGeneration { get; set; } = true;
	[ConVar.Replicated( "sv_paf_treeamount", Help = "Maximum tree amount of post apocalyptic forestation", Max = 5000, Min = 0, Saved = true )]
	public static int treeAmount { get; set; } = 1000;
	[ConVar.Replicated( "sv_paf_rockamount", Help = "Maximum rock amount of post apocalyptic forestation", Max = 500, Min = 0, Saved = true )]
	public static int rockAmount { get; set; } = 100;
	[ConVar.Replicated( "sv_paf_grassamount", Help = "Maximum grass amount of post apocalyptic forestation", Max = 6000, Min = 0, Saved = true )]
	public static int grassAmount { get; set; } = 5000;
	[ConVar.Replicated( "sv_paf_ivyamount", Help = "Maximum ivy amount of post apocalyptic forestation", Max = 3000, Min = 0, Saved = true )]
	public static int ivyAmount { get; set; } = 2000;
	[ConVar.Replicated( "sv_paf_debrisamount", Help = "Maximum debris amount of post apocalyptic forestation", Max = 1000, Min = 0, Saved = true )]
	public static int debrisAmount { get; set; } = 1000;

	static readonly string[] TreeModels = new string[]
	{
		"models/rust_nature/overgrowth/creeping_tree_a.vmdl",
		"models/rust_nature/overgrowth/creeping_tree_b.vmdl",
		"models/rust_nature/overgrowth/creeping_tree_c.vmdl",
		"models/rust_nature/overgrowth/creeping_tree_d.vmdl",
		"models/rust_nature/overgrowth/creeping_tree_e.vmdl",
		"models/rust_nature/overgrowth/creeping_tree_f.vmdl",
		"models/rust_nature/overgrowth/creeping_tree_g.vmdl",
		"models/rust_nature/overgrowth/creeping_tree_conifer_a.vmdl",
		"models/sbox_props/trees/horse_chestnut/horse_chestnut.vmdl",
		"models/sbox_props/trees/oak/tree_oak_big_a.vmdl",
		"models/sbox_props/trees/oak/tree_oak_big_b.vmdl",
		"models/sbox_props/trees/oak/tree_oak_medium_a.vmdl",
		"models/sbox_props/trees/oak/tree_oak_small_a.vmdl",
		"models/rust_nature/american_beech/american_beech_a.vmdl",
		"models/rust_nature/american_beech/american_beech_a_dead.vmdl",
		"models/rust_nature/american_beech/american_beech_b.vmdl",
		"models/rust_nature/american_beech/american_beech_c.vmdl",
		"models/rust_nature/american_beech/american_beech_d.vmdl",
		"models/rust_nature/american_beech/american_beech_d_dead.vmdl",
		"models/rust_nature/american_beech/american_beech_e.vmdl",
		"models/rust_nature/american_beech/american_beech_e_dead.vmdl"
	};

	static readonly string[] GrassModels = new string[]
	{
		"models/sbox_props/nature/grass_clumps/grass_clump_a.vmdl",
		"models/rust_nature/overgrowth/patch_grass_small.vmdl",
		"models/rust_nature/overgrowth/patch_grass_short.vmdl",
		"models/rust_nature/overgrowth/patch_grass_medium.vmdl",
		"models/rust_nature/overgrowth/cracks_grass_a.vmdl",
		"models/rust_nature/overgrowth/cracks_grass_b.vmdl",
		"models/rust_nature/overgrowth/cracks_grass_c.vmdl",
		"models/rust_nature/overgrowth/cracks_grass_d.vmdl",
		"models/rust_nature/overgrowth/cracks_grass_e.vmdl",
		"models/rust_nature/overgrowth/cracks_grass_f.vmdl",
		"models/rust_nature/overgrowth/cracks_grass_g.vmdl",
		"models/rust_nature/overgrowth/cracks_grass_h.vmdl",
		"models/rust_nature/overgrowth/cracks_grass_i.vmdl",
		"models/rust_nature/overgrowth/cracks_grass_j.vmdl",
		"models/rust_nature/overgrowth/patch_grass_tall.vmdl",
		"models/rust_nature/overgrowth/patch_grass_tall_large.vmdl",
		"models/sbox_props/nature/grass_clumps/grass_clump_a.vmdl",
		"models/sbox_props/nature/grass_clumps/grass_clump_b.vmdl",
		"models/sbox_props/nature/grass_clumps/grass_clump_c.vmdl"
	};

	static readonly string[] BushModels = new string[]
	{
		"models/sbox_props/shrubs/beech/beech_bush_large.vmdl",
		"models/sbox_props/shrubs/beech/beech_bush_medium.vmdl",
		"models/sbox_props/shrubs/beech/beech_bush_medium_wall_corner.vmdl",
		"models/sbox_props/shrubs/beech/beech_bush_regular_medium_a.vmdl",
		"models/sbox_props/shrubs/beech/beech_bush_regular_medium_b.vmdl",
		"models/sbox_props/shrubs/beech/beech_bush_regular_small.vmdl",
		"models/sbox_props/shrubs/beech/beech_bush_small.vmdl",
		"models/sbox_props/shrubs/beech/beech_shrub_tall_large.vmdl",
		"models/sbox_props/shrubs/beech/beech_shrub_tall_medium.vmdl",
		"models/sbox_props/shrubs/beech/beech_shrub_tall_small.vmdl",
		"models/sbox_props/shrubs/beech/beech_shrub_wide_large.vmdl",
		"models/sbox_props/shrubs/beech/beech_shrub_wide_medium.vmdl",
		"models/sbox_props/shrubs/beech/beech_shrub_wide_small.vmdl"
	};

	static readonly string[] FlowerModels = new string[]
	{
		"models/rust_nature/overgrowth/patch_flowers_a.vmdl",
		"models/rust_nature/overgrowth/patch_flowers_b.vmdl",
		"models/rust_nature/overgrowth/patch_flowers_c.vmdl",
		"models/rust_nature/overgrowth/patch_flowers_d.vmdl"
	};

	static readonly string[] ReedModels = new string[]
	{
		"models/rust_nature/reeds/reed_medium.vmdl",
		"models/rust_nature/reeds/reed_medium_blossom.vmdl",
		"models/rust_nature/reeds/reed_medium_dry.vmdl",
		"models/rust_nature/reeds/reed_small.vmdl",
		"models/rust_nature/reeds/reed_small_dry.vmdl",
		"models/rust_nature/reeds/reed_smaller.vmdl",
		"models/rust_nature/reeds/reed_tall_blossom.vmdl",
		"models/rust_nature/reeds/reeds_dead.vmdl",
		"models/rust_nature/reeds/reeds_medium.vmdl",
		"models/rust_nature/reeds/reeds_small.vmdl",
		"models/rust_nature/reeds/reeds_small_dry.vmdl",
		"models/rust_nature/reeds/reeds_small_sparse.vmdl",
		"models/rust_nature/reeds/reeds_tall.vmdl",
		"models/rust_nature/reeds/reeds_tall_dry.vmdl",
	};

	static readonly string[] IvyModels = new string[]
	{
		"models/rust_nature/overgrowth/creeping_plant_a.vmdl",
		"models/rust_nature/overgrowth/creeping_plant_b.vmdl",
		"models/rust_nature/overgrowth/creeping_plant_fall_a.vmdl",
		"models/rust_nature/overgrowth/creeping_plant_fence_600.vmdl",
		"models/rust_nature/overgrowth/creeping_plant_wall_600.vmdl",
		"models/rust_nature/overgrowth/creeping_plant_wall_900.vmdl"
		//"models/rust_nature/overgrowth/creeping_plant_corner_a.vmdl",
		//"models/rust_nature/overgrowth/creeping_plant_corner_b.vmdl",
		//"models/rust_nature/overgrowth/creeping_plant_corner_c.vmdl",
		//"models/rust_nature/overgrowth/creeping_plant_pipe_100x600.vmdl",
		//"models/rust_nature/overgrowth/creeping_plant_pipe_200x600.vmdl",
		//"models/rust_nature/overgrowth/creeping_plant_pipe_300x600.vmdl"

	};

	static readonly string[] RockModels = new string[]
	{
		"models/rust_nature/rocks/rock_cliff_a.vmdl",
		"models/rust_nature/rocks/rock_cliff_b.vmdl",
		"models/rust_nature/rocks/rock_cliff_c.vmdl",
		"models/rust_nature/rocks/rock_ledge_a.vmdl",
		"models/rust_nature/rocks/rock_med_a.vmdl",
		"models/rust_nature/rocks/rock_med_b.vmdl",
		"models/rust_nature/rocks/rock_med_c.vmdl",
		"models/rust_nature/rocks/rock_quarry_a.vmdl",
		"models/rust_nature/rocks/rock_quarry_small_a.vmdl",
		"models/rust_nature/rocks/rock_quarry_small_b.vmdl",
		"models/rust_nature/rocks/rock_small_a.vmdl",
		"models/rust_nature/rocks/rock_small_a_cave.vmdl",
		"models/rust_nature/rocks/rock_small_b.vmdl",
		"models/rust_nature/rocks/rock_small_b_cave.vmdl",
		"models/rust_nature/rocks/rock_small_c.vmdl",
		"models/rust_nature/rocks/rock_small_c_cave.vmdl"
	};

	static readonly string[] DebrisModels = new string[]
	{
		"models/rust_props/concrete_debris/concrete_debris_a.vmdl",
		"models/rust_props/concrete_debris/concrete_debris_b.vmdl",
		"models/rust_props/concrete_debris/concrete_debris_c.vmdl",
		"models/rust_props/small_junk/bottle_cluster_a.vmdl",
		"models/rust_props/small_junk/bottle_cluster_b.vmdl",
		"models/rust_props/small_junk/bottle_cluster_c.vmdl",
		"models/rust_props/small_junk/bottle_cluster_d.vmdl",
		"models/rust_props/small_junk/can_cluster_a.vmdl",
		"models/rust_props/small_junk/can_cluster_b.vmdl",
		"models/rust_props/small_junk/cola_can_cluster_a.vmdl",
		"models/rust_props/small_junk/crisps_cluster_a.vmdl",
		"models/rust_props/small_junk/ground_cloth_a.vmdl",
		"models/rust_props/small_junk/ground_junk_a.vmdl",
		"models/rust_props/small_junk/ground_junk_straight_a.vmdl",
		"models/rust_props/small_junk/jar_cluster_a.vmdl",
		"models/rust_props/small_junk/jar_cluster_b.vmdl"
		//"models/rust_props/small_junk/milk_carton_cluster_a.vmdl",
	};

	struct EntInfo
	{
		public string Model;
		public float Scale;
		public Vector3 Position;
		public Rotation Rotation;
		public Color RenderColor;
	}

	PostApocalypticForestation()
	{
		if ( Host.IsClient )
			ClientJoinedHandler();

		Log.Info( $"[sbox.community] Post Apocalyptic Forestation is loaded!, Auto-generation is {(autoGeneration?"enable":"disable")}" );
	}

	private static void applyEffects()
	{
		if ( defaultFog is null )
			defaultFog = Map.Scene.GradientFog;

		if ( defaultAmbientColor is null )
			defaultAmbientColor = Map.Scene.AmbientLightColor;

		Map.Scene.AmbientLightColor = "#4c5610";
		//Map.Camera.BackgroundColor = "#4c5610";

		Map.Scene.GradientFog.Enabled = true;
		Map.Scene.GradientFog.Color = "#4c5610";
		Map.Scene.GradientFog.MaximumOpacity = 0.05f;
		Map.Scene.GradientFog.StartHeight = 10;
		Map.Scene.GradientFog.EndHeight = 200;
		Map.Scene.GradientFog.DistanceFalloffExponent = 10;
		Map.Scene.GradientFog.VerticalFalloffExponent = 2;
		Map.Scene.GradientFog.StartDistance = 500;
		Map.Scene.GradientFog.EndDistance = 3000;
	}

	[ConCmd.Admin( "sv_paf_createeffects" )]
	public static void CreateEffects()
	{
		applyEffects();
		CreateEffectsCL( To.Everyone );
	}

	[ClientRpc]
	public static void CreateEffectsCL()
	{
		applyEffects();
	}

	private static async Task SpawnGrasses_thread(int maxray, int amount, List<Vector3> hitpositions, List<Vector3> hitnormals )
	{
		var mapbounds = Map.Physics.Body.GetBounds();
		var mapheight = Map.Physics.Body.GetBounds().Size.z;

		var founded = 0;
		int delaystep = 500; //batch
		int delaycount = 0;

		for ( int i = 0; i < maxray; i++ )
		{
			if ( delaycount == delaystep )
			{
				await GameTask.Delay( 10 );
				delaycount = 0;

				if ( _debug )
					Log.Info( $"Checked {i}. points" );
			}

			delaycount++;

			if ( founded >= amount )
				break;

			var randomPoint = mapbounds.RandomPointInside;

			var angle = Vector3.Down * mapheight;
			angle += randomPoint;

			//Ground checking
			var tr = Trace.Ray( randomPoint, angle )
				.WorldOnly()
				.Run();

			if ( !tr.Hit )
				continue;

			if ( _debug )
				Log.Info( "Hit.." );

			//Water checking
			if ( Trace.TestPoint( tr.HitPosition, "water", 10f ) )
				continue;

			if ( _debug )
				Log.Info( "Water not found.." );

			//Angles checking for spawning horizontally
			if ( tr.Normal.Angle( Vector3.Up ) > 25.0f )
				continue;

			if ( _debug )
				Log.Info( "Angles good.." );

			//Distance checking between them
			if ( spawnedGrasses.Any( x => x.Position.DistanceSquared( tr.HitPosition ) < 5 * 5 ) )
				continue;

			hitpositions.Add( tr.HitPosition );
			hitnormals.Add( tr.Normal );

			++founded;

		}
	}

	[ConCmd.Admin( "sv_paf_spawngrassess" )]
	public static async void SpawnGrasses()
	{
		spawnedGrasses ??= new List<ModelEntity>();

		clearForestCL( grasses: true );

		spawnedGrasses.Clear();
		doneGrasses.Clear();

		List<Vector3> hitpositions = new();
		List<Vector3> hitnormals = new();

		await SpawnGrasses_thread( grassAmount * 10, grassAmount, hitpositions , hitnormals);

		var count = hitpositions.Count;

		for (var i=0; i< count; i++)
		{
			if ( _debug )
				Log.Info( "Preparing grass; " + hitpositions[i] );

			doneGrasses.Add( new EntInfo()
			{
				Model = i < count * 0.9f ? Rand.FromArray( GrassModels ) : Rand.FromArray( (Rand.Int(1,2) == 1) ? BushModels : ((Rand.Int(1, 2) == 1) ? FlowerModels : ReedModels)),
				Scale = Rand.Float( i < count * 0.9f ? 0.7f : 0.4f, i < count * 0.9f ? 1.2f : 0.8f ),
				Position = hitpositions[i],
				Rotation = Rotation.LookAt( hitnormals[i] + Vector3.Random * 0.2f, Vector3.Random ) * Rotation.From( 90, 0, 0 ),
				RenderColor = Color.Lerp( "#4c5610", "#c79852", Rand.Float( 0.0f, 1f ) ),
			});
		}

		SendGrasses( To.Everyone );

		Log.Info( $"[PAF-SV] Prepared {count} grasses!" );
	}

	public static void SendGrasses( To to )
	{
		using ( var stream = new MemoryStream() )
		{
			using ( var writer = new BinaryWriter( stream ) )
			{
				writer.Write( doneGrasses.Count );

				foreach ( var item in doneGrasses )
				{
					writer.Write( item.Model );
					writer.Write( item.Scale.ToString() ); //float problem?
					writer.Write( item.Position.ToString());
					writer.Write( item.Rotation.ToString() );
					writer.Write( item.RenderColor.ToString(true,true) );
				}

				SendGrassesToCL( to, stream.ToArray() );
			}
		}
	}

	[ClientRpc]
	public static void SendGrassesToCL( byte[] data )
	{
		spawnedGrasses ??= new List<ModelEntity>();

		foreach ( var grass in spawnedGrasses )
			grass.Delete();

		spawnedGrasses.Clear();

		using ( var stream = new MemoryStream( data ) )
		{
			using ( var reader = new BinaryReader( stream ) )
			{
				var count = reader.ReadInt32();

				for ( var i = 0; i < count; i++ )
				{
					var model = reader.ReadString();
					var scale = float.Parse(reader.ReadString());
					var position = Vector3.Parse(reader.ReadString());
					var rotation = Rotation.Parse(reader.ReadString());
					var rendercolor = Color.Parse(reader.ReadString());

					var ent = new ModelEntity();
					ent.Model = Model.Load( model );
					ent.Scale = scale;
					ent.Position = position;
					ent.Rotation = rotation;
					ent.RenderColor = rendercolor.GetValueOrDefault();

					spawnedGrasses.Add( ent );
				}

				Log.Info( $"[PAF-CL] Created {count} grasses!" );
			}
		}
	}

	[ConCmd.Admin( "sv_paf_spawntrees" )]
	public static void SpawnTrees()
	{
		spawnedTrees ??= new List<ModelEntity>();

		foreach ( var trees in spawnedTrees )
			trees.Delete();

		spawnedTrees.Clear();

		var mapbounds = Map.Physics.Body.GetBounds();
		var mapheight = Map.Physics.Body.GetBounds().Size.z;

		for ( int i = 0; i < treeAmount; i++ )
		{
			var randomPoint = mapbounds.RandomPointInside;

			//z-axis for ground checking
			var angle = Vector3.Down * mapheight;
			angle += randomPoint;

			//z-axis for sky checking
			var angleSky = Vector3.Up * mapheight * 10;
			angleSky += randomPoint;

			//z-axis for underground space checking
			//var angleGround = Vector3.Down * 15f;
			//angle += CurrentView.Rotation.Forward;

			//Ground checking
			var tr = Trace.Ray( randomPoint, angle )
				.WorldOnly()
				.Size( new Vector3( 5, 5, 25 ) )//to detect small areas
				.Radius( 150f )//to detect small areas
				.Run();

			if ( !tr.Hit )
				continue;

			if ( _debug )
				Log.Info( "Hit.." );

			//Sky checking
			var trSky = Trace.Ray( randomPoint, angleSky )
				.WorldOnly()
				.Run();

			if ( trSky.Hit )
				continue;

			if ( _debug )
				Log.Info( "Sky detected.." );

			//Water checking
			if ( Trace.TestPoint( tr.HitPosition, "water", 100f ) )
				continue;

			if ( _debug )
				Log.Info( "Water not found.." );

			//Underground space checking
			/*var trEmptySpaceGround = Trace.Ray( randomPoint- angleGround, randomPoint + angleGround )
				.WorldOnly()
				.Run();

			if ( !trEmptySpaceGround.Hit )
				continue;

			if( _debug )
				Log.Info( "Underground has not space.." );
			 */

			//Angles checking for spawning horizontally
			if ( tr.Normal.Angle( Vector3.Up ) > 25.0f )
				continue;

			if ( _debug )
				Log.Info( "Angles good.." );

			//Distance checking between them
			if ( spawnedTrees.Any( x => x.Position.DistanceSquared( tr.HitPosition ) < 200 * 200 ) )
				continue;

			if ( _debug )
				Log.Info( "Creating tree; " + tr.HitPosition );

			var ent = new ModelEntity();
			ent.Model = Model.Load( Rand.FromArray( TreeModels ) );
			ent.Scale = Rand.Float( 0.7f, 2.6f );
			ent.SetupPhysicsFromModel( PhysicsMotionType.Static );
			ent.Position = tr.HitPosition;
			ent.Rotation = Rotation.LookAt( Vector3.Up + Vector3.Random * 0.4f, Vector3.Random ) * Rotation.From( 90, 0, 0 );
			ent.RenderColor = Color.Lerp( "#4c5610", "#c79852", Rand.Float( 0.0f, 1f ) );
			spawnedTrees.Add( ent );
		}

		Log.Info( $"[PAF-SV] Created {treeAmount} trees!" );

	}

	[ConCmd.Admin( "sv_paf_spawnrocks" )]
	public static void SpawnRocks()
	{
		spawnedRocks ??= new List<ModelEntity>();

		foreach ( var rock in spawnedRocks )
			rock.Delete();

		spawnedRocks.Clear();

		var mapbounds = Map.Physics.Body.GetBounds();
		var mapheight = Map.Physics.Body.GetBounds().Size.z;

		for ( int i = 0; i < rockAmount; i++ )
		{
			var randomPoint = mapbounds.RandomPointInside;

			//z-axis for ground checking
			var angle = Vector3.Down * mapheight;
			angle += randomPoint;

			//z-axis for sky checking
			var angleSky = Vector3.Up * mapheight * 10;
			angleSky += randomPoint;

			//z-axis for underground space checking
			//var angleGround = Vector3.Down * 15f;
			//angle += CurrentView.Rotation.Forward;

			//Ground checking
			var tr = Trace.Ray( randomPoint, angle )
				.WorldOnly()
				.Size( new Vector3( 5, 5, 25 ) )//to detect small areas
				.Radius( 150f )//to detect small areas
				.Run();

			if ( !tr.Hit )
				continue;

			if ( _debug )
				Log.Info( "Hit.." );

			//Sky checking
			var trSky = Trace.Ray( randomPoint, angleSky )
				.WorldOnly()
				.Run();

			if ( trSky.Hit )
				continue;

			if ( _debug )
				Log.Info( "Sky detected.." );

			//Water checking
			if ( Trace.TestPoint( tr.HitPosition, "water", 100f ) )
				continue;

			if ( _debug )
				Log.Info( "Water not found.." );

			//Angles checking for spawning horizontally
			if ( tr.Normal.Angle( Vector3.Up ) > 25.0f )
				continue;

			if ( _debug )
				Log.Info( "Angles good.." );

			//Distance checking between them
			if ( spawnedRocks.Any( x => x.Position.DistanceSquared( tr.HitPosition ) < 200 * 200 ) )
				continue;

			if ( _debug )
				Log.Info( "Creating rock; " + tr.HitPosition );

			var ent = new ModelEntity();
			ent.Model = Model.Load( Rand.FromArray( RockModels ) );
			ent.Scale = Rand.Float( 0.3f, 0.7f );
			ent.Position = tr.HitPosition;
			ent.SetupPhysicsFromModel( PhysicsMotionType.Static );
			ent.Rotation = Rotation.LookAt( Vector3.Up + Vector3.Random * 0.4f, Vector3.Random ) * Rotation.From( 90, 0, 0 );
			ent.RenderColor = Color.Lerp( "#4c5610", "#c79852", Rand.Float( 0.0f, 1f ) );
			spawnedRocks.Add( ent );

		}

		Log.Info( $"[PAF-SV] Created {rockAmount} rocks!" );
	}

	private static async Task SpawnIvys_thread( int maxray, int amount, List<Vector3> hitpositions, List<Vector3> hitnormals )
	{
		var mapbounds = Map.Physics.Body.GetBounds();

		var founded = 0;
		int delaystep = 500; //batch
		int delaycount = 0;

		for ( int i = 0; i < maxray; i++ )
		{

			if ( delaycount == delaystep )
			{
				await GameTask.Delay( 10 );
				delaycount = 0;

				if ( _debug )
					Log.Info( $"Checked {i}. points" );
			}

			delaycount++;

			if ( founded >= amount )
				break;

			var randomPoint = mapbounds.RandomPointInside;

			var angle = Vector3.Down * 0.5f;//Vector3.Random + Vector3.Down * 0.5f;
			angle += randomPoint;

			//Ground checking
			var tr = Trace.Ray( randomPoint, randomPoint + angle * 1000 )
				.WorldOnly()
				.Run();

			if ( !tr.Hit )
				continue;

			if ( _debug )
				Log.Info( "Hit.." );

			//Angles checking for spawning vertically
			if ( tr.Normal.Angle( Vector3.Up ) < 25.0f )
				continue;

			if ( _debug )
				Log.Info( "Angles good.." );

			//Distance checking between them
			if ( spawnedIvys.Any( x => x.Position.Distance( tr.HitPosition ) < 40 ) )
				continue;

			hitpositions.Add( tr.HitPosition );
			hitnormals.Add( tr.Normal );

			++founded;

		}
	}

	[ConCmd.Admin( "sv_paf_spawnivys" )]
	public static async void SpawnIvys()
	{
		spawnedIvys ??= new List<ModelEntity>();

		clearForestCL( ivys: true );

		spawnedIvys.Clear();
		doneIvys.Clear();

		List<Vector3> hitpositions = new();
		List<Vector3> hitnormals = new();

		await SpawnIvys_thread( ivyAmount * 10, ivyAmount, hitpositions, hitnormals );

		var count = hitpositions.Count;

		for ( var i = 0; i < count; i++ )
		{
			if ( _debug )
				Log.Info( "Preparing ivy; " + hitpositions[i] );


			doneIvys.Add( new EntInfo()
			{
				Model = Rand.FromArray( IvyModels ),
				Scale = Rand.Float( 0.7f, 1.2f ),
				Position = hitpositions[i],
				Rotation = Rotation.LookAt( hitnormals[i] + Vector3.Random * 0.4f, Vector3.Random ) * Rotation.From( 0, 0, 90 ), //review
				RenderColor = Color.Lerp( "#4c5610", "#c79852", Rand.Float( 0.0f, 1f ) ),
			} );

		}

		SendIvys( To.Everyone );

		Log.Info( $"[PAF-SV] Prepared {count} ivys!" );
	}

	private static void SendIvys( To to )
	{
		using ( var stream = new MemoryStream() )
		{
			using ( var writer = new BinaryWriter( stream ) )
			{
				writer.Write( doneIvys.Count );

				foreach ( var item in doneIvys )
				{
					writer.Write( item.Model );
					writer.Write( item.Scale.ToString() ); //float problem?
					writer.Write( item.Position.ToString() );
					writer.Write( item.Rotation.ToString() );
					writer.Write( item.RenderColor.ToString( true, true ) );
				}

				SendIvysToCL( to, stream.ToArray() );
			}
		}
	}

	[ClientRpc]
	public static void SendIvysToCL( byte[] data )
	{
		spawnedIvys ??= new List<ModelEntity>();

		foreach ( var ivy in spawnedIvys )
			ivy.Delete();

		spawnedIvys.Clear();

		using ( var stream = new MemoryStream( data ) )
		{
			using ( var reader = new BinaryReader( stream ) )
			{
				var count = reader.ReadInt32();

				for ( var i = 0; i < count; i++ )
				{
					var model = reader.ReadString();
					var scale = float.Parse( reader.ReadString() );
					var position = Vector3.Parse( reader.ReadString() );
					var rotation = Rotation.Parse( reader.ReadString() );
					var rendercolor = Color.Parse( reader.ReadString() );

					var ent = new ModelEntity();
					ent.Model = Model.Load( model );
					ent.Scale = scale;
					ent.Position = position;
					ent.Rotation = rotation;
					ent.RenderColor = rendercolor.GetValueOrDefault();

					spawnedIvys.Add( ent );
				}

				Log.Info( $"[PAF-CL] Created {count} Ivys!" );
			}
		}
	}


	private static async Task SpawnDebris_thread( int maxray, int amount, List<Vector3> hitpositions, List<Vector3> hitnormals )
	{
		var mapbounds = Map.Physics.Body.GetBounds();
		var mapheight = Map.Physics.Body.GetBounds().Size.z;

		var founded = 0;
		int delaystep = 500; //batch
		int delaycount = 0;

		for ( int i = 0; i < maxray; i++ )
		{

			if ( delaycount == delaystep )
			{
				await GameTask.Delay( 10 );
				delaycount = 0;

				if ( _debug )
					Log.Info( $"Checked {i}. points" );
			}

			delaycount++;

			if ( founded >= amount )
				break;

			var randomPoint = mapbounds.RandomPointInside;

			//z-axis for ground checking
			var angle = Vector3.Down * mapheight;
			angle += randomPoint;

			//z-axis for sky checking
			var angleSky = Vector3.Up * mapheight * 10;
			angleSky += randomPoint;


			//Ground checking
			var tr = Trace.Ray( randomPoint, randomPoint + angle * 1000 )
				.WorldOnly()
				.Run();

			if ( !tr.Hit )
				continue;

			if ( _debug )
				Log.Info( "Hit.." );

			//Sky checking
			var trSky = Trace.Ray( randomPoint, angleSky )
				.WorldOnly()
				.Run();

			if ( !trSky.Hit )
				continue;

			if ( _debug )
				Log.Info( "Sky not detected.." );

			//Water checking
			if ( Trace.TestPoint( tr.HitPosition, "water", 100f ) )
				continue;

			if ( _debug )
				Log.Info( "Water not found.." );

			//Angles checking for spawning horizontally
			if ( tr.Normal.Angle( Vector3.Up ) > 25.0f )
				continue;

			if ( _debug )
				Log.Info( "Angles good.." );

			//Distance checking between them
			if ( spawnedDebris.Any( x => x.Position.DistanceSquared( tr.HitPosition ) < 100 * 100 ) )
				continue;

			hitpositions.Add( tr.HitPosition );
			hitnormals.Add( tr.Normal );

			++founded;

		}
	}

	[ConCmd.Admin( "sv_paf_spawndebris" )]
	public static async void SpawnDebris()
	{

		spawnedDebris ??= new List<ModelEntity>();

		clearForestCL( debris: true );

		spawnedDebris.Clear();
		doneDebris.Clear();

		List<Vector3> hitpositions = new();
		List<Vector3> hitnormals = new();

		await SpawnDebris_thread( debrisAmount * 10, debrisAmount, hitpositions, hitnormals );

		var count = hitpositions.Count;

		for ( var i = 0; i < count; i++ )
		{
			if ( _debug )
				Log.Info( "Preparing debris; " + hitpositions[i] );

			doneDebris.Add( new EntInfo()
			{
				Model = Rand.FromArray( DebrisModels ),
				Scale = Rand.Float( 0.8f, 1.2f ),
				Position = hitpositions[i],
				Rotation = Rotation.LookAt( hitnormals[i] + Vector3.Random * 0.1f, Vector3.Random ) * Rotation.From( 90, 0, 0 ), 
				RenderColor = Color.Lerp( "#4c5610", "#c79852", Rand.Float( 0.0f, 1f ) ),
			} );
		}

		SendDebris( To.Everyone );

		Log.Info( $"[PAF-SV] Prepared {count} debris!" );
	}

	private static void SendDebris( To to )
	{
		using ( var stream = new MemoryStream() )
		{
			using ( var writer = new BinaryWriter( stream ) )
			{
				writer.Write( doneDebris.Count );

				foreach ( var item in doneDebris )
				{
					writer.Write( item.Model );
					writer.Write( item.Scale.ToString() ); //float problem?
					writer.Write( item.Position.ToString() );
					writer.Write( item.Rotation.ToString() );
					writer.Write( item.RenderColor.ToString( true, true ) );
				}

				SendDebrisToCL( to, stream.ToArray() );
			}
		}
	}

	[ClientRpc]
	public static void SendDebrisToCL( byte[] data )
	{
		spawnedDebris ??= new List<ModelEntity>();

		foreach ( var debris in spawnedDebris )
			debris.Delete();

		spawnedDebris.Clear();

		using ( var stream = new MemoryStream( data ) )
		{
			using ( var reader = new BinaryReader( stream ) )
			{
				var count = reader.ReadInt32();

				for ( var i = 0; i < count; i++ )
				{
					var model = reader.ReadString();
					var scale = float.Parse( reader.ReadString() );
					var position = Vector3.Parse( reader.ReadString() );
					var rotation = Rotation.Parse( reader.ReadString() );
					var rendercolor = Color.Parse( reader.ReadString() );

					var ent = new ModelEntity();
					ent.Model = Model.Load( model );
					ent.Scale = scale;
					ent.Position = position;
					ent.Rotation = rotation;
					ent.RenderColor = rendercolor.GetValueOrDefault();

					spawnedDebris.Add( ent );
				}

				Log.Info( $"[PAF-CL] Created {count} Debris!" );
			}
		}
	}

	[ConCmd.Admin( "sv_paf_generateforest" )]
	[PostCleanup]
	public static void SpawnForest()
	{
		SpawnTrees(); //svside
		SpawnRocks(); //svside
		SpawnGrasses(); //clside
		SpawnIvys(); //clside
		SpawnDebris(); //clside
		CreateEffects();//shared
	}

	[PostSpawn]
	private static void paf_autoGeneration()
	{
		var ratio = (Map.Physics.Body.GetBounds().Size.x * Map.Physics.Body.GetBounds().Size.y) / 130347680f; //based construct map

		if (Host.IsServer)
			Log.Info( $"[PAF] Map scale calculated as {ratio}" );

		ratio += 0.1f; //boosted

		treeAmount = Math.Min((int) (1000 * ratio), 5000);
		rockAmount  = Math.Min( (int)(100 * ratio), 500 );
		grassAmount  = Math.Min( (int)(5000 * ratio), 6000 );
		ivyAmount  = Math.Min( (int)(2000 * ratio), 3000 );
		debrisAmount = Math.Min( (int)(1000 * ratio), 1000 );

		if ( Host.IsServer )
			Log.Info( $"[PAF] Calculated as Sufficiently for '{Map.Name}'; Tree: {treeAmount}, Rock: {rockAmount}, Grass: {grassAmount}, Ivy: {ivyAmount}, Debris: {debrisAmount}" );

		if (Host.IsServer && autoGeneration )
			SpawnForest();
	}

	[ConCmd.Admin( "sv_paf_clearforest" )]
	[PreCleanup]
	public static void ClearForest()
	{
		if ( spawnedTrees != null )
		{
			foreach ( var trees in spawnedTrees )
				trees.Delete();

			Log.Info( $"[PAF-SV] Removed {spawnedTrees.Count} trees!" );

			spawnedTrees.Clear();
		}

		if ( doneGrasses != null )
			doneGrasses.Clear();

		if ( doneIvys != null )
			doneIvys.Clear();

		if ( doneDebris != null )
			doneDebris.Clear();

		if ( spawnedRocks != null )
		{
			foreach ( var rock in spawnedRocks )
				rock.Delete();

			Log.Info( $"[PAF-SV] Removed {spawnedRocks.Count} rocks!" );

			spawnedRocks.Clear();
		}

		if ( defaultFog is not null )
			Map.Scene.GradientFog = defaultFog.GetValueOrDefault();

		if ( defaultAmbientColor is not null )
			Map.Scene.AmbientLightColor = defaultAmbientColor.GetValueOrDefault();

		clearForestCL(To.Everyone, grasses: true, ivys: true, debris: true, effects: true);
	}

	[ClientRpc]
	public static void clearForestCL( bool grasses= false, bool ivys = false, bool debris = false, bool effects = false)
	{
		if ( grasses && spawnedGrasses != null )
		{
			foreach ( var grass in spawnedGrasses )
				grass.Delete();

			Log.Info( $"[PAF-CL] Removed {spawnedGrasses.Count} grasses!" );

			spawnedGrasses.Clear();
		}

		if ( ivys && spawnedIvys != null )
		{
			foreach ( var ivy in spawnedIvys )
				ivy.Delete();

			Log.Info( $"[PAF-CL] Removed {spawnedIvys.Count} ivys!" );

			spawnedIvys.Clear();
		}

		if ( debris && spawnedDebris != null )
		{
			foreach ( var debr in spawnedDebris )
				debr.Delete();

			Log.Info( $"[PAF-CL] Removed {spawnedDebris.Count} debris!" );

			spawnedDebris.Clear();
		}

		if( effects )
		{
			if ( defaultFog is not null )
				Map.Scene.GradientFog = defaultFog.GetValueOrDefault();

			if ( defaultAmbientColor is not null )
				Map.Scene.AmbientLightColor = defaultAmbientColor.GetValueOrDefault();
		}
	}

	// Events and game's overrides can't accessible?

	[ConCmd.Server]
	public static void ClientJoinedHandler() 
	{
		var client = ConsoleSystem.Caller;
		
		if ( spamProtect.TryGetValue( client.PlayerId, out var cl ) )
		{
			if (cl.Equals(client))
				return;
			else
			{
				spamProtect.Remove( client.PlayerId );
				spamProtect.Add( client.PlayerId, client );
			}
		}
		else
			spamProtect.Add( client.PlayerId, client );

		SendGrasses( To.Single( client ) );
		SendIvys( To.Single( client ) );
		SendDebris( To.Single( client ) );
		CreateEffectsCL( To.Single( client ) );
	}

	[Event.Frame]
	public static void ClientJoinedHandlerCL()
	{
		if( !joinedCL )
		{
			joinedCL = true;
			ClientJoinedHandler();
		}
	}
}
