/*
 * Creado por SharpDevelop.
 * Usuario: Emilio
 * Fecha: 21/03/2008
 * Hora: 19:42
 * 
 * Para cambiar esta plantilla use Herramientas | Opciones | Codificación | Editar Encabezados Estándar
 */

using System;
using System.Data;
using System.Text;
using NUnit.Framework;
using TodoASql;
using Modelador;

namespace Indices
{
	public class CampoProducto:CampoChar{ public CampoProducto():base(4){} };
	public class CampoNombre:CampoChar{ public CampoNombre():base(250){} };
	public class CampoAgrupacion:CampoChar{ public CampoAgrupacion():base(9){} };
	public class CampoGrupo:CampoChar{ public CampoGrupo():base(9){} };
	public class CampoPonderador:CampoReal{};
	public class CampoNivel:CampoEntero{}
	public class CampoPrecio:CampoReal{};
	public class CampoIndice:CampoReal{};
	public class CampoFactor:CampoReal{};
	public class CampoPeriodo:CampoChar{ public CampoPeriodo():base(4+2){} }
	public class RepositorioIndice:Repositorio
	{
		public class Productos:Tabla{
			[Pk] public CampoProducto cProducto;
			public CampoNombre	cNombreProducto;
			public double Ponderador(Grupos grupo){
				Grupos hoja=new Grupos();
				hoja.Leer(grupo.db,grupo.cAgrupacion,cProducto);
				return hoja.cPonderador.Valor/grupo.cPonderador.Valor;
			}
		}
		public class Grupos:Tabla{
			[Pk] public CampoAgrupacion cAgrupacion;
			[Pk] public CampoGrupo cGrupo;
			public CampoNombre cNombreGrupo;
			public CampoGrupo cGrupoPadre;
			public CampoPonderador cPonderador;
			public CampoNivel cNivel;
			public CampoLogico cEsProducto;
		}
		[Vista]
		public class Agrupaciones:Grupos{
			
		}
		public class Numeros:Tabla{
			[Pk] public CampoEntero cNumero;
		}
		public class AuxGrupos:Tabla{
			[Pk] public CampoAgrupacion cAgrupacion;
			[Pk] public CampoGrupo cGrupo;
			public CampoPonderador cPonderadorOriginal;
			public CampoPonderador cSumaPonderadorHijos;	
		}
		public class Periodos:Tabla{
			[Pk] public CampoPeriodo cPeriodo;
			public CampoPeriodo cPeriodoAntrr;
			public CampoEntero cAno;
			public CampoEntero cMes;
		}
		public class CalPro:Tabla{
			[Pk] public CampoPeriodo cPeriodo;
			[Pk] public CampoProducto cProducto;
			public CampoPrecio cPromedio;
		}
		public class CalGru:Tabla{
			[Pk] public CampoPeriodo cPeriodo;
			[Pk] public CampoAgrupacion cAgrupacion;
			public CampoGrupo cGrupo;
			public CampoIndice cIndice;
			public CampoFactor cFactor;
		}
		RepositorioIndice(BaseDatos db)
			:base(db)
		{
		}
		public static RepositorioIndice Crear(BaseDatos db){
			RepositorioIndice rta=new RepositorioIndice(db);
			rta.CrearTablas();
			return rta;
		}
		public override void CrearTablas(){
			base.CrearTablas();
			for(int i=0; i<=20; i++){
				db.ExecuteNonQuery("insert into numeros (numero) values ("+i.ToString()+")");
			}
		}
		public static RepositorioIndice Abrir(BaseDatos db){
			return new RepositorioIndice(db);			
		}
		public Productos AbrirProducto(string codigo){
			Productos p=new Productos();
			p.Leer(db,codigo);
			return p;
		}
		public Productos CrearProducto(string codigo){
			Productos p=new Productos();
			using(Insertador ins=p.Insertar(db)){
				p.cProducto[ins]=codigo;
			}
			return AbrirProducto(codigo);
		}
		public Grupos AbrirGrupo(string agrupacion,string codigo){
			Grupos g=new Grupos();
			g.Leer(db,agrupacion,codigo);
			return g;
		}
		public Grupos CrearGrupo(string codigo){
			return CrearGrupo(codigo,null,1);
		}
		public Grupos CrearGrupo(string codigo,Grupos padre,double ponderador){
			Grupos g=new Grupos();
			string agrupacion;
			using(Insertador ins=g.Insertar(db)){
				g.cGrupo[ins]=codigo;
				g.cPonderador[ins]=ponderador;
				if(padre==null){
					agrupacion=codigo;
				}else{
					agrupacion=padre.cAgrupacion.Valor;
					g.cGrupoPadre[ins]=padre.cGrupo;
				}
				g.cAgrupacion[ins]=agrupacion;
				// ins.InsertarSiHayCampos();
			}
			return AbrirGrupo(agrupacion,codigo);
		}
		public void CrearHoja(Productos producto,Grupos grupo,double ponderador){
			Grupos g=new Grupos();
			using(Insertador ins=g.Insertar(db)){
				g.cAgrupacion[ins]=grupo.cAgrupacion;
				g.cGrupo[ins]=producto.cProducto;
				g.cGrupoPadre[ins]=grupo.cGrupo;
				g.cPonderador[ins]=ponderador;
				g.cEsProducto[ins]=db.Verdadero;
				// ins.InsertarSiHayCampos();
			}			
		}
		public Periodos CrearPeriodo(int ano, int mes){
			Periodos p=new Periodos();
			using(Insertador ins=p.Insertar(db)){
				p.cPeriodo[ins]=ano.ToString()+mes.ToString("00");
				p.cAno[ins]=ano;
				p.cMes[ins]=mes;
			}
			return AbrirPeriodo(ano,mes);
		}
		public Periodos AbrirPeriodo(int ano, int mes){
			Periodos p=new Periodos();
			p.Leer(db,"ano",ano,"mes",mes);
			return p;
		}
		public void RegistrarPromedio(Periodos per,Productos prod,double promedio){
			CalPro t=new CalPro();
			using(Insertador ins=t.Insertar(db)){
				t.cPeriodo[ins]=per.cPeriodo;
				t.cProducto[ins]=prod.cProducto;
				t.cPromedio[ins]=promedio;
				// ins.InsertarSiHayCampos();
			}			
		}
		public void CalcularPonderadores(Grupos grupo){
			using(EjecutadorSql ej=db.Ejecutador("agrupacion",grupo.cAgrupacion.Valor)){
				ej.ExecuteNonQuery(@"
					UPDATE grupos SET nivel=0,ponderador=1
					  WHERE grupopadre is null 
					    AND agrupacion={agrupacion};
				");
				for(int i=0;i<10;i++){
				ej.ExecuteNonQuery(new SentenciaSql(db,@"
					UPDATE grupos SET nivel={nivel}+1
					  WHERE (grupopadre) 
						IN (SELECT grupo 
                             FROM grupos 
                             WHERE nivel={nivel} 
                               AND agrupacion={agrupacion})
                        AND agrupacion={agrupacion}
				").Arg("nivel",i));
				}
				for(int i=9;i>=0;i--){ // Subir ponderadores nulos
					if(db.GetType()==typeof(BdAccess)){
						ej.ExecuteNonQuery(new SentenciaSql(db,@"
							UPDATE grupos p SET p.ponderador=
							    DSum('ponderador','grupos','grupopadre=''' & grupo & ''' and agrupacion=''' & agrupacion & '''')
							  WHERE agrupacion={agrupacion} 
		                        AND nivel={nivel}
		                        AND ponderador IS NULL
						").Arg("nivel",i));
					}else{
						ej.ExecuteNonQuery(new SentenciaSql(db,@"
							UPDATE grupos SET ponderador=
							    (SELECT sum(h.ponderador)
							       FROM grupos h
							       WHERE h.grupopadre=grupos.grupo
							         AND h.agrupacion=grupos.agrupacion)
							  WHERE agrupacion={agrupacion} 
		                        AND nivel={nivel}
		                        AND ponderador IS NULL
						").Arg("nivel",i));
					}
				}
				for(int i=1;i<10;i++){
					ej.ExecuteNonQuery(new SentenciaSql(db,@"
						INSERT INTO auxgrupos (agrupacion,grupo,ponderadororiginal,sumaponderadorhijos)
						  SELECT p.agrupacion,p.grupo,p.ponderador,sum(h.ponderador)
						    FROM grupos p INNER JOIN grupos h ON p.agrupacion=h.agrupacion AND p.grupo=h.grupopadre
						    WHERE h.agrupacion={agrupacion} 
	                          AND h.nivel={nivel}
						    GROUP BY p.agrupacion,p.grupo,p.ponderador;
					").Arg("nivel",i));
					if(db.GetType()==typeof(BdAccess)){
						ej.ExecuteNonQuery(new SentenciaSql(db,@"
							UPDATE grupos h INNER JOIN auxgrupos a ON a.grupo=h.grupopadre AND a.agrupacion=h.agrupacion
                              SET h.ponderador=h.ponderador*a.ponderadororiginal/a.sumaponderadorhijos
							  WHERE h.agrupacion={agrupacion} 
		                        AND h.nivel={nivel}
						").Arg("nivel",i));
					}else{
						ej.ExecuteNonQuery(new SentenciaSql(db,@"
							UPDATE grupos SET ponderador=ponderador
							    *(SELECT a.ponderadororiginal/a.sumaponderadorhijos
							       FROM auxgrupos a 
							       WHERE a.grupo=grupos.grupopadre
							         AND a.agrupacion=grupos.agrupacion)
							  WHERE agrupacion={agrupacion} 
		                        AND nivel={nivel}
						").Arg("nivel",i));
					}
				}
			}
		}
		public void CalcularMesBase(Periodos per,Agrupaciones agrupacion){
			using(EjecutadorSql ej=db.Ejecutador("periodo",per.cPeriodo.Valor,"agrupacion",agrupacion.cAgrupacion.Valor)){
				ej.ExecuteNonQuery(@"
					insert into calgru (ano,mes,agrupacion,grupo,indice,factor)
					  select {ano},{mes},agrupacion,grupo,100,1
					    from grupos
					    where agrupacion={agrupacion};
				");
			}
		}
		public void CalcularCalGru(Periodos periodo,Agrupaciones agrupacion){
			using(EjecutadorSql ej=db.Ejecutador("periodo",periodo.cPeriodo.Valor,"agrupacion",agrupacion.cAgrupacion.Valor)){
				/*
				ej.ExecuteNonQuery(@"
					insert into calgru (ano,mes,agrupacion,grupo,indice,factor)
					  select per.ano,per.mes,g.agrupacion,g.grupo,cg0.indice*cp1.promedio/cp0.promedio,cg0.factor
					    from ((((grupos as g 
							 inner join productos as p on g.grupo=p.producto)
					         inner join calgru as cg0 on g.grupo=cg0.grupo and g.agrupacion=cg0.agrupacion)
					         inner join periodos as per on per.anoant=cg0.ano and per.mesant=cg0.mes)
					         inner join calpro as cp0 on cp0.ano=per.anoant and cp0.mes=per.mesant and cp0.producto=p.producto)
					         inner join calpro as cp1 on cp1.ano=per.ano and cp1.mes=per.mes and cp1.producto=p.producto
					    where g.agrupacion={agrupacion}
					      and per.ano={ano}
					      and per.mes={mes}
				");
				*/
				ej.ExecuteNonQuery(@"
					insert into calgru (ano,mes,agrupacion,grupo,indice,factor)
					  select per.ano,per.mes,g.agrupacion,g.grupo,cg0.indice*cp1.promedio/cp0.promedio,cg0.factor
					    from grupos as g, 
							 productos as p,
					         calgru as cg0,
					         periodos as per, 
					         calpro as cp0, 
					         calpro as cp1
					    where g.agrupacion={agrupacion}
					      and per.ano={ano}
					      and per.mes={mes}
					      and g.grupo=p.producto
					      and g.grupo=cg0.grupo and g.agrupacion=cg0.agrupacion
					      and per.anoant=cg0.ano and per.mesant=cg0.mes
					      and cp0.ano=per.anoant and cp0.mes=per.mesant and cp0.producto=p.producto
					      and cp1.ano=per.ano and cp1.mes=per.mes and cp1.producto=p.producto
				");
				for(int i=9;i>=0;i--){
					/*
					ej.ExecuteNonQuery(new SentenciaSql(db,@"
						insert into calgru (ano,mes,agrupacion,grupo,indice,factor)
						  select cg.ano,cg.mes,gp.agrupacion,gp.grupo
								,sum(cg.indice*gh.ponderador)/sum(gh.ponderador)
								,sum(cg.factor*gh.ponderador)/sum(gh.ponderador)
						    from (grupos gh
						         inner join calgru as cg on gh.grupo=cg.grupo and gh.agrupacion=cg.agrupacion)
						         inner join grupos gp on gp.agrupacion=gh.agrupacion and gp.grupo=gh.grupopadre
						    where cg.agrupacion={agrupacion}
						      and cg.ano={ano}
						      and cg.mes={mes}
						      and gh.nivel={nivel}
						    group by cg.ano,cg.mes,gp.agrupacion,gp.grupo;
					").Arg("nivel",i));
					*/
					ej.ExecuteNonQuery(new SentenciaSql(db,@"
						insert into calgru (ano,mes,agrupacion,grupo,indice,factor)
						  select cg.ano,cg.mes,gp.agrupacion,gp.grupo
								,sum(cg.indice*gh.ponderador)/sum(gh.ponderador)
								,sum(cg.factor*gh.ponderador)/sum(gh.ponderador)
						    from grupos gh,
						         calgru as cg,
						         grupos gp 
						    where cg.agrupacion={agrupacion}
						      and cg.ano={ano}
						      and cg.mes={mes}
						      and gh.nivel={nivel}
						      and gh.grupo=cg.grupo and gh.agrupacion=cg.agrupacion
						      and gp.agrupacion=gh.agrupacion and gp.grupo=gh.grupopadre
						    group by cg.ano,cg.mes,gp.agrupacion,gp.grupo;
					").Arg("nivel",i));
				}
			}
		}
		public void ReglasDeIntegridad(){
			db.AssertSinRegistros(
				"La raiz de los grupos en una canasta debe tener el mismo código de la agrupación que define",
			@"
				SELECT *
				  FROM grupos
				  WHERE grupopadre is null and grupo<>agrupacion
			");
			db.AssertSinRegistros(
				"Solo la raiz de los grupos en una canasta debe tener el mismo código de la agrupación que define",
			@"
				SELECT *
				  FROM grupos
				  WHERE grupopadre is not null and grupo=agrupacion
			");
			db.AssertSinRegistros(
				"Todos los grupos deben tener nivel",
			@"
				SELECT *
				  FROM grupos
				  WHERE nivel is null
			");
			db.AssertSinRegistros(
				"La raiz de los grupos debe tener nivel 0",
			@"
				SELECT *
				  FROM grupos
				  WHERE grupopadre is null and nivel<>0
			");
			db.AssertSinRegistros(
				"Los niveles de los hijos deben ser exactamente 1 más que el padre",
			@"
				SELECT h.grupopadre, p.nivel as nivelpadre, h.grupo, h.nivel, p.nombregrupo as nombrepadre, h.nombregrupo, p.nivel+1-h.nivel
				  FROM grupos p inner join grupos h on p.grupo=h.grupopadre and p.agrupacion=h.agrupacion
				  WHERE p.nivel+1<>h.nivel
			");
			db.AssertSinRegistros(
				"Todas las agrupaciones que no son productos deben tener hijos",
			@"
				SELECT p.grupo,p.nombregrupo
				  FROM grupos p left join grupos h on p.grupo=h.grupopadre and p.agrupacion=h.agrupacion
				  WHERE p.esproducto='N'
				    AND h.grupo is null
			");
			db.AssertSinRegistros(
				"Todas las agrupaciones que son productos no deben tener hijos",
			@"
				SELECT p.grupo,p.nombregrupo,h.grupo as grupohijo,h.nombregrupo as nombrehijo
				  FROM grupos p left join grupos h on p.grupo=h.grupopadre and p.agrupacion=h.agrupacion
				  WHERE p.esproducto='S'
				    AND h.grupo is not null
			");
			db.AssertSinRegistros(
				"La suma de los ponderadores debe ser igual al poderador del padre",
			@"
				SELECT p.grupo,p.nombregrupo,p.ponderador,sum(h.ponderador)
				  FROM grupos p left join grupos h on p.grupo=h.grupopadre and p.agrupacion=h.agrupacion
				  GROUP BY p.grupo,p.nombregrupo,p.ponderador
				  HAVING abs(p.ponderador)-sum(h.ponderador)>0.00000000000001
			");
			db.AssertSinRegistros(
				"Solo deben ser hojas los productos",
			@"
				SELECT g.grupo, g.nombregrupo, p.producto, p.nombreproducto
				  FROM grupos g inner join productos p ON g.grupo=p.producto
				  WHERE g.esproducto='N'
			");
			db.AssertSinRegistros(
				"Si esta marcado como producto debe existir el producto",
			@"
				SELECT g.grupo, g.nombregrupo, p.producto, p.nombreproducto
				  FROM grupos g left join productos p ON g.grupo=p.producto
				  WHERE g.esproducto='S' AND p.producto is null
			");
			db.AssertSinRegistros(
				"Los ponderadores de cada nivel deben sumar 1",
			@"
				SELECT g.agrupacion,n.numero as nivel, sum(ponderador) as suma, sum(ponderador)-1 as diferencia
				  FROM numeros as n, grupos as g
				  WHERE g.nivel=n.numero OR g.esproducto='S' AND g.nivel<n.numero
				  GROUP BY g.agrupacion,n.numero
				  HAVING sum(ponderador)<>1;
			");
			db.AssertSinRegistros(
				"No debe haber dos códigos de grupo iguales en distintas agrupaciones",
			@"
				SELECT grupo, count(*) as cantidad, min(agrupacion) as primero, max(agrupacion) as ultimo
				  FROM grupos as g
				  WHERE esproducto='N'
				  GROUP BY grupo
				  HAVING count(*)>1
			");
			db.AssertSinRegistros(
				"No debe haber hijos sin padre",
			@"
				SELECT h.grupopadre,h.grupo,h.nombregrupo
				  FROM grupos h left join grupos p on p.grupo=h.grupopadre and p.agrupacion=h.agrupacion
				  WHERE p.grupo IS NULL and h.grupopadre IS NOT NULL;
			");
		}
	}
	/*
	public class TablaDB:IDisposable{
		protected RepositorioIndice Repo;
		string NombreTabla;
		protected IDataReader Registro;
		public System.Collections.Generic.List<Clave> Claves;
		StringBuilder Sentencia;
		protected TablaDB(RepositorioIndice repo,string nombreTabla,params object[] clavesPlanas){
			this.Repo=repo;	
			this.NombreTabla=nombreTabla;
			this.Claves=new System.Collections.Generic.List<Clave>();
			Sentencia=new StringBuilder("SELECT * FROM "+repo.db.StuffTabla(nombreTabla));
			int i=0;
			while(i<clavesPlanas.Length){
				object parametro=clavesPlanas[i];
				if(parametro.GetType()==typeof(string)){
					AgregarClave((string)parametro,clavesPlanas[i+1]);
					i++; 
				}else if(parametro.GetType().IsSubclassOf(typeof(TablaDB))){
					TablaDB t=(TablaDB)parametro;
					foreach(Clave c in t.Claves){
						AgregarClave(c.Campo,c.Valor);
					}
				}
				i++;
			}
			this.Registro=repo.db.ExecuteReader(Sentencia.ToString());
			this.Registro.Read();
		}
		public object this[string campo]{ 
			get{
				return Registro[campo];
			}
		}
		public void Dispose(){
			Registro.Close();
		}
		protected void AgregarClave(string Campo, object Valor){
			string and=(Claves.Count==0?" WHERE ":" AND ");
			Claves.Add(new Clave(Campo,Valor));
			Sentencia.Append(and+Repo.db.StuffCampo(Campo)+"="+Repo.db.StuffValor(Valor));
		}
		public struct Clave{
			public string Campo;
			public object Valor;
			public Clave(string campo, object valor){ this.Campo=campo; this.Valor=valor; } 
		}
	}
	public class Grupo:TablaDB{
		public double Ponderador;
		public string Agrupacion;
		public new string Clave;
		public Grupo(RepositorioIndice repo,string agrupacion,string grupo)
			:base(repo,"grupos","agrupacion",agrupacion,"grupo",grupo)
		{
			Clave=(string)Registro["grupo"];
			if(Registro["agrupacion"]!=null){
				Agrupacion=(string)Registro["agrupacion"];
			}
			if(Registro["ponderador"].GetType()!=typeof(DBNull)){
				object valor=Registro["ponderador"];
				System.Console.WriteLine("tipo = "+valor.GetType().Name);
				Ponderador=(double)Registro["ponderador"];
			}
		}
	}
	public class Agrupacion:Grupo{
		public Agrupacion(RepositorioIndice repo,string agrupacion)
			:base(repo,agrupacion,agrupacion)
		{}
	}
	public class Producto:TablaDB{
		public new string Clave;
		public Producto(RepositorioIndice repo,string codigo)
			:base(repo,"productos","producto",codigo)
		{
			Clave=(string)Registro["producto"];
		}
		public double Ponderador(Grupo grupo){
			double wP=(double)
				Repo.db.ExecuteScalar(@"
					SELECT ponderador
					  FROM grupos 
					  WHERE agrupacion="+Repo.db.StuffValor(grupo.Agrupacion)+@"
					    AND grupo="+Repo.db.StuffValor(Clave)
				);
			double wG=(double)
				Repo.db.ExecuteScalar(@"
					SELECT ponderador
					  FROM grupos 
					  WHERE agrupacion="+Repo.db.StuffValor(grupo.Agrupacion)+@"
					    AND grupo="+Repo.db.StuffValor(grupo.Clave)
				);
			return wP/wG;
		}
	}
	public class Periodo:TablaDB{
		public int Ano;
		public int Mes;
		public Periodo(RepositorioIndice repo,int ano,int mes)
			:base(repo,"periodos","ano",ano,"mes",mes)
		{
			Ano=(int)Registro["ano"];
			Mes=(int)Registro["mes"];
		}
		public static Periodo Crear(RepositorioIndice repo,int ano,int mes,object anoant,object mesant){
			using(InsertadorSql ins=new InsertadorSql(repo.db,"periodos")){
				ins["ano"]=ano;
				ins["mes"]=mes;
				ins["anoant"]=anoant;
				ins["mesant"]=mesant;
				// ins.InsertarSiHayCampos();
			}
			return new Periodo(repo,ano,mes);
		}
		public static Periodo Crear(RepositorioIndice repo,int ano,int mes){
			return Crear(repo,ano,mes,null,null);
		}
		public static Periodo CrearProximo(RepositorioIndice repo,Periodo anterior){
			return Crear(repo,(anterior.Mes==12?anterior.Ano+1:anterior.Ano)
			             ,(anterior.Mes==12?1:anterior.Mes+1),anterior.Ano,anterior.Mes);
		}
	}
	public class CalGru:TablaDB{
		public double Indice;
		public CalGru(RepositorioIndice repo,Periodo per,Grupo grupo)
			:base(repo,"calgru",per,grupo)
		{
			Indice=(double)Registro["indice"];
		}
	}
	*/
	[TestFixture]
	public class ProbarIndiceD3{
		RepositorioIndice repo;
		public ProbarIndiceD3(){
			BaseDatos db;
			switch(2){
				case 1: // probar con postgre
					db=PostgreSql.Abrir("127.0.0.1","import2sqlDB","import2sql","sqlimport");
					db.EliminarTablaSiExiste("calgru");
					db.EliminarTablaSiExiste("calpro");
					db.EliminarTablaSiExiste("periodos");
					db.EliminarTablaSiExiste("grupos");
					db.EliminarTablaSiExiste("productos");
					db.EliminarTablaSiExiste("numeros");
					db.EliminarTablaSiExiste("auxgrupos");
					break;
				case 2: // probar con sqlite
					string archivoSqLite="prueba_sqlite.db";
					Archivo.Borrar(archivoSqLite);
					db=SqLite.Abrir(archivoSqLite);
					break;
				case 3: // probar con access
					string archivoMDB="indices_canastaD3.mdb";
					Archivo.Borrar(archivoMDB);
					BdAccess.Crear(archivoMDB);
					db=BdAccess.Abrir(archivoMDB);
					break;
			}
			repo=RepositorioIndice.Crear(db);
			RepositorioIndice.Productos P100=repo.CrearProducto("P100");
			RepositorioIndice.Productos P101=repo.CrearProducto("P101");
			RepositorioIndice.Productos P102=repo.CrearProducto("P102");
			RepositorioIndice.Grupos A=repo.CrearGrupo("A");
			RepositorioIndice.Grupos A1=repo.CrearGrupo("A1",A,60);
			RepositorioIndice.Grupos A2=repo.CrearGrupo("A2",A,40);
			RepositorioIndice.Grupos T=repo.CrearGrupo("T");
			repo.CrearHoja(P100,A1,60);
			repo.CrearHoja(P101,A1,40);
			repo.CrearHoja(P102,A2,100);
			repo.CrearHoja(P100,T,60);
			repo.CrearHoja(P101,T,40);
			repo.CrearHoja(P102,T,100);
			repo.CalcularPonderadores(A);
			repo.CalcularPonderadores(T);
		}
		[Test]
		public void VerCanasta(){
			RepositorioIndice.Grupos A=repo.AbrirGrupo("A","A");
			RepositorioIndice.Grupos A1=repo.AbrirGrupo("A","A1");
			RepositorioIndice.Productos P100=repo.AbrirProducto("P100");
			Assert.AreEqual(1.0,A.cPonderador.Valor);
			Assert.AreEqual(0.6,A1.cPonderador.Valor,0.00000001);
			Assert.AreEqual(0.36,P100.Ponderador(A));
			Assert.AreEqual(0.6,P100.Ponderador(A1));
		}
		[Test]
		public void A01CalculosBase(){
			/*
			RepositorioIndice.Periodos pAnt=repo.CrearPeriodo(2001,12);
			RepositorioIndice.Productos P100=repo.AbrirProducto("P100");
			RepositorioIndice.Productos P101=repo.AbrirProducto("P101");
			RepositorioIndice.Productos P102=repo.AbrirProducto("P102");
			RepositorioIndice.Agrupaciones A=repo.AbrirAgrupacion("A");
			repo.RegistrarPromedio(pAnt,P100,2.0);
			repo.RegistrarPromedio(pAnt,P101,10.0);
			repo.RegistrarPromedio(pAnt,P102,20.0);
			repo.CalcularMesBase(pAnt,A);
			Assert.AreEqual(100.0,new CalGru(repo,pAnt,A).Indice);
			Periodo Per1=Periodo.CrearProximo(repo,pAnt);
			Assert.AreEqual(2001,Per1["anoant"]);
			Assert.AreEqual(2002,Per1.Ano);
			Assert.AreEqual(1,Per1.Mes);
			Grupo A1=repo.AbrirGrupo("A","A1");
			Grupo A2=repo.AbrirGrupo("A","A2");
			repo.RegistrarPromedio(Per1,P100,2.0);
			repo.RegistrarPromedio(Per1,P101,10.0);
			repo.RegistrarPromedio(Per1,P102,22.0);
			repo.CalcularCalGru(Per1,A);
			Assert.AreEqual(110.0,new CalGru(repo,Per1,A2).Indice);
			Assert.AreEqual(104.0,new CalGru(repo,Per1,A).Indice);
			Periodo Per2=Periodo.CrearProximo(repo,Per1);
			repo.RegistrarPromedio(Per2,P100,2.2);
			repo.RegistrarPromedio(Per2,P101,11.0);
			repo.RegistrarPromedio(Per2,P102,22.0);
			repo.CalcularCalGru(Per2,A);
			Assert.AreEqual(110.0,new CalGru(repo,Per2,A2).Indice);
			Assert.AreEqual(110.0,new CalGru(repo,Per2,A).Indice);
			*/
		}
		[Test]
		public void zReglasDeIntegridad(){
			repo.ReglasDeIntegridad();
		}
	}
}
