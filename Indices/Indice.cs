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

using Comunes;
using BasesDatos;
using Modelador;

namespace Indices
{
	public class EjecutadorSql:BasesDatos.EjecutadorSql{
		public EjecutadorSql(BaseDatos db,Parametros[] param)
			:base(db,param)
		{
		}
		public EjecutadorSql(BaseDatos db,params object[] paramPlanos)
			:base(db,paramPlanos)
		{
		}
		/*
		protected override string AdaptarSentecia(TodoASql.SentenciaSql sentencia)
		{
			return base.AdaptarSentecia(
				sentencia.ToString()
				.Replace("grupos#filtro","grupos.agrupacion={agrupacion}")
			);
		}
		*/
	}
	public class RepositorioIndice:Repositorio
	{
		public RepositorioIndice(BaseDatos db)
			:base(db)
		{
		}
		public override void CrearTablas(){
			CrearTablas(db,this.GetType().Namespace);
			for(int i=0; i<=20; i++){
				db.ExecuteNonQuery("INSERT INTO numeros (numero) VALUES ("+i.ToString()+");");
			}
		}
		public override void EliminarTablas(){
			EliminarTablas(db,this.GetType().Namespace);
		}
		public void CalcularPonderadores(Agrupaciones agrupacion){
			using(Ejecutador ej=new Ejecutador(db,agrupacion)){
				Grupos grupos=new Grupos();
				Grupos hijos=new Grupos();
				hijos.UsarFk();
				Grupos padre=hijos.fkGrupoPadre;
				ej.Ejecutar(
					new SentenciaUpdate(grupos,grupos.cNivel.Set(0),grupos.cPonderador.Set(1.0))
					.Where(grupos.cGrupoPadre.EsNulo())); 
				for(int i=0;i<10;i++){
					ej.Ejecutar(
						new SentenciaUpdate(grupos,grupos.cNivel.Set(i+1))
						.Where(grupos.InPadresWhere(grupos.cNivel.Igual(i)))
					);
				}
				for(int i=9;i>=0;i--){ // Subir ponderadores nulos
					ej.Ejecutar(
						new SentenciaUpdate(padre,padre.cPonderador.Set(padre.SelectSuma(hijos.cPonderador)))
						.Where(padre.cNivel.Igual(i).And(padre.cPonderador.EsNulo()))
					);
				}
				AuxGrupos auxgrupos=new AuxGrupos();
				for(int i=1;i<10;i++){
					ej.Ejecutar(
						new SentenciaInsert(new AuxGrupos())
						.Select(padre.cAgrupacion,padre.cGrupo,auxgrupos.cPonderadorOriginal.Es(padre.cPonderador),auxgrupos.cSumaPonderadorHijos.EsSuma(hijos.cPonderador))
						.Where(hijos.cNivel.Igual(i))
					);
					AuxGrupos aux=new AuxGrupos();
					aux.EsFkDe(hijos,hijos.cGrupoPadre);
					ej.Ejecutar(
						new SentenciaUpdate(hijos,hijos.cPonderador.Set(hijos.cPonderador.Por(aux.cPonderadorOriginal.Dividido(aux.cSumaPonderadorHijos))))
						.Where(hijos.cNivel.Igual(i))
					);
				}
			}
		}
		public void CalcularPonderadores(string agrupacion){
			Agrupaciones a=new Agrupaciones();
			a.Leer(db,"C");
			CalcularPonderadores(a);
		}
		public void CalcularMesBase(Calculos cal,Agrupaciones agrupacion){
			using(Ejecutador ej=new Ejecutador(db,agrupacion,cal)){
				CalGru cg=new CalGru();
				Grupos g=new Grupos();
				ej.Ejecutar(
					new SentenciaInsert(cg)
					.Select(cg.cPeriodo.Es(cal.cPeriodo.Valor),cg.cCalculo.Es(cal.cCalculo.Valor),g.cAgrupacion,g.cGrupo,cg.cIndice.Es(100.0),cg.cFactor.Es(1.0))
				);
			}
		}
		public void CalcularCalGru(Calculos cal,Agrupaciones agrupacion){
			using(Ejecutador ej=new Ejecutador(db,agrupacion,cal)){
				// Tabla a insertar:
				CalGru cg=new CalGru();
				// Tabla base:
				CalProd cp=new CalProd();
				cp.UsarFk();
				Calculos c=cp.fkCalculos;
				CalProd cp0=new CalProd();
				cp0.EsFkDe(cp,cp0.cPeriodo.Es(c.cPeriodoAnterior));
				cp0.LiberadaDelContextoDelEjecutador=true;
				CalGru cg0=new CalGru();
				cg0.EsFkDe(cp0,cg0.cGrupo.Es(cp0.cProducto),cg0.cAgrupacion.Es(agrupacion.cAgrupacion.Valor));
				cg0.LiberadaDelContextoDelEjecutador=true;
				ej.Ejecutar(
					new SentenciaInsert(cg)
					.Select(c,cg0.cAgrupacion,cg0.cGrupo,cg.cIndice.Es(cg0.cIndice.Por(cp.cPromedio.Dividido(cp0.cPromedio))),cg0.cFactor)
				);
				Grupos gh=new Grupos();
				cg.EsFkDe(gh,cg.cPeriodo.Es(cal.cPeriodo.Valor),cg.cCalculo.Es(cal.cCalculo.Valor));
				Grupos gp=new Grupos();
				gp.EsFkDe(gh,gp.cGrupo.Es(gh.cGrupoPadre));
				CalGru cgp=new CalGru();
				for(int i=9;i>=0;i--){
					ej.Ejecutar(
						new SentenciaInsert(cgp)
						.Select(c,gp.cAgrupacion,gp.cGrupo,
						        cgp.cIndice.Es(ExpresionSql.Sum(cg.cIndice.Por(gh.cPonderador)).Dividido(ExpresionSql.Sum(gh.cPonderador))),
						        cgp.cFactor.Es(ExpresionSql.Sum(cg.cFactor.Por(gh.cPonderador)).Dividido(ExpresionSql.Sum(gh.cPonderador))))
						.Where(gh.cNivel.Igual(i))
					);
				}
			}
		}
		public void CalcularMatrizBase(int CantidadPeriodosMinima){
			{
				NovEspInf n=new NovEspInf();
				Calculos c=new Calculos();
				RelVar rv=new RelVar();
				rv.UsarFk();
				Variedades v=rv.fkVariedades;
				c.EsFkDe(rv,c.cCalculo.Es(-1));
				NovEspInf nss=new NovEspInf();
				nss.SubSelect(rv.cPeriodo,c.cCalculo,rv.cInformante,v.cEspecificacion)
				.Where(c.cEsPeriodoBase.Igual(true),rv.cPrecio.NoEsNulo())
				.GroupBy();
				new Ejecutador(db).Ejecutar(
					new SentenciaInsert(n)
					.Select(n.cPeriodo.EsMax(nss.cPeriodo),nss.cCalculo,nss.cInformante,nss.cEspecificacion,n.cEstado.Es(NovEspInf.Estados.Alta))
					.Having(n.cPeriodo.EsCount(nss.cPeriodo).MayorOIgual(CantidadPeriodosMinima))
				);
			}
			Calculos cals=new Calculos();
			foreach(Calculos cal in new Calculos().Algunos(db,cals.cEsPeriodoBase.Igual(true),cals.cPeriodo.Desc())){
				using(Ejecutador ej=new Ejecutador(db,cal)){
					NovEspInf nei=new NovEspInf();
					CalEspInf cei=new CalEspInf();
					CalEspInf cei0=new CalEspInf();
					Calculos c=new Calculos();
					RelVar rv=new RelVar();
					c.EsFkDe(cei0,cei0.cPeriodo.Es(c.cPeriodoAnterior));
					ej.Ejecutar(
						new SentenciaInsert(cei)
						.Select(c.cPeriodo,
								cei.cAntiguedadConPrecio.Es(cei0.cAntiguedadConPrecio.Mas(1)),
						        cei.cAntiguedadSinPrecio.Es(cei0.cAntiguedadSinPrecio.Mas(1)),
						        cei0)
					);
					ej.Ejecutar(
						new SentenciaInsert(cei)
						.Select(nei)
						.Where(nei.NotIn(cei))
					);
					ej.Ejecutar(
						new SentenciaUpdate(cei,cei.cPromedio.Set(cei.SelectPromedioGeometrico(rv.cPrecio)))
					);
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
				  HAVING abs(sum(ponderador)-1)>0.000000001;
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
	public class RepositorioPruebaIndice:RepositorioIndice{
		public RepositorioPruebaIndice(BaseDatos db)
			:base(db)
		{}
		public override void CrearTablas(){
			RepositorioIndice repo=new RepositorioIndice(db);
			repo.CrearTablas();
		}
		public override void EliminarTablas(){
			RepositorioIndice repo=new RepositorioIndice(db);
			repo.EliminarTablas();
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
		public Agrupaciones AbrirAgrupacion(string agrupacion){
			Agrupaciones a=new Agrupaciones();
			a.Leer(db,agrupacion);
			return a;
		}
		public Grupos CrearGrupo(string codigo){
			return CrearGrupo(codigo,codigo,"",1);
		}
		public Grupos CrearGrupo(string agrupacion,string codigo,string codigopadre,double ponderador){
			Grupos g=new Grupos();
			using(Insertador ins=g.Insertar(db)){
				g.cGrupo[ins]=codigo;
				g.cPonderador[ins]=ponderador;
				if(codigopadre==""){
				}else{
					g.cGrupoPadre[ins]=codigopadre;
				}
				g.cAgrupacion[ins]=agrupacion;
			}
			return AbrirGrupo(agrupacion,codigo);
		}
		public Grupos CrearGrupo(string codigo,Grupos padre,double ponderador){
			return CrearGrupo(padre.cAgrupacion.Valor,codigo,padre.cGrupo.Valor,ponderador);
		}
		public Grupos CrearGrupo(string codigo,Agrupaciones raiz,double ponderador){
			return CrearGrupo(raiz.cAgrupacion.Valor,codigo,raiz.cAgrupacion.Valor,ponderador);
		}
		public Agrupaciones CrearAgrupacion(string codigo){
			Agrupaciones a=new Agrupaciones();
			using(Insertador ins=a.Insertar(db)){
				a.cAgrupacion[ins]=codigo;
			}
			CrearGrupo(codigo,codigo,null,1);
			return AbrirAgrupacion(codigo);
		}
		public void CrearHoja(Productos producto,Grupos grupo,double ponderador){
			Grupos g=new Grupos();
			using(Insertador ins=g.Insertar(db)){
				g.cAgrupacion[ins]=grupo.cAgrupacion;
				g.cGrupo[ins]=producto.cProducto;
				g.cGrupoPadre[ins]=grupo.cGrupo;
				g.cPonderador[ins]=ponderador;
				g.cEsProducto[ins]=db.Verdadero;
			}			
		}
		public static Periodos CrearPeriodo(BaseDatos db,int ano, int mes){
			Periodos p=new Periodos();
			using(Insertador ins=p.Insertar(db)){
				p.cPeriodo[ins]=ano.ToString()+mes.ToString("00");
				p.cAno[ins]=ano;
				p.cMes[ins]=mes;
			}
			return new Periodos(db,ano,mes);
		}
		public Periodos CrearPeriodo(int ano, int mes){
			return CrearPeriodo(db,ano,mes);
		}
		public Periodos CrearProximo(Periodos ant){
			int ano=ant.cAno.Valor;
			int mes=ant.cMes.Valor+1;
			if(mes==13){
				mes=1; 
				ano++;
			}
			Periodos p=new Periodos();
			string codigoNuevo=ano.ToString()+((int)mes).ToString("00");
			if(!p.Buscar(db,codigoNuevo)){
				using(Insertador ins=p.Insertar(db)){
					p.cPeriodo[ins]=codigoNuevo;
					p.cAno[ins]=ano;
					p.cMes[ins]=mes;
				}
			}
			return new Periodos(db,ano,mes);
		}
		public Periodos AbrirPeriodo(int ano, int mes){
			Periodos p=new Periodos();
			p.LeerNoPk(db,"ano",ano,"mes",mes);
			return p;
		}
		public Calculos CrearCalculo(int ano, int mes, int calculo){
			Periodos p=CrearPeriodo(ano,mes);
			Calculos c=new Calculos();
			using(Insertador ins=c.Insertar(db)){
				c.cPeriodo[ins]=p.cPeriodo.Valor;
				c.cCalculo[ins]=calculo;
			}
			return AbrirCalculo(ano,mes,calculo);
		}
		public Calculos AbrirCalculo(int ano, int mes, int calculo){
			Periodos p=AbrirPeriodo(ano,mes);
			Calculos c=new Calculos();
			c.Leer(db,p.cPeriodo.Valor,calculo);
			return c;
		}
		public Calculos CrearProximo(Calculos ant){
			Periodos p=new Periodos();
			p.Leer(db,ant.cPeriodo);
			Periodos pProx=CrearProximo(p);
			Calculos c=new Calculos();
			using(Insertador ins=c.Insertar(db)){
				c.cPeriodo[ins]=pProx.cPeriodo.Valor;
				c.cCalculo[ins]=ant.cCalculo.Valor;
				c.cPeriodoAnterior[ins]=ant.cPeriodo;
				c.cEsPeriodoBase[ins]=ant.cEsPeriodoBase;
			}
			c.Leer(db,pProx.cPeriodo.Valor,ant.cCalculo.Valor);
			return c;
		}
		public void RegistrarPromedio(Calculos cal,Productos prod,double promedio){
			CalProd t=new CalProd();
			using(Insertador ins=t.Insertar(db)){
				t.cPeriodo[ins]=cal.cPeriodo;
				t.cCalculo[ins]=cal.cCalculo;
				t.cProducto[ins]=prod.cProducto;
				t.cPromedio[ins]=promedio;
			}		
		}
		public void ExpandirEspecificacionesYVariedades(){
			Productos p=new Productos();
			Especificaciones e=new Especificaciones();
			Variedades v=new Variedades();
			Ejecutador ej=new Ejecutador(db);
			ej.Ejecutar(
				new SentenciaInsert(e).Select(e.cEspecificacion.Es(p.cProducto),p.cProducto)
			);
			ej.Ejecutar(
				new SentenciaInsert(v).Select(v.cVariedad.Es(e.cEspecificacion),e.cEspecificacion)
			);
		}
	}
	[TestFixture]
	public class ProbarIndiceD3{
		RepositorioPruebaIndice repo;
		public ProbarIndiceD3(){
			BaseDatos db;
			#pragma warning disable 162
			switch(1){ 
				case 1: // probar con postgre
					db=PostgreSql.Abrir("127.0.0.1","import2sqlDB","import2sql","sqlimport");
					repo=new RepositorioPruebaIndice(db);
					repo.EliminarTablas();
					repo.CrearTablas();
					break;
				case 2: // probar con sqlite
					string archivoSqLite="prueba_sqlite.db";
					Archivo.Borrar(archivoSqLite);
					db=SqLite.Abrir(archivoSqLite);
					repo=new RepositorioPruebaIndice(db);
					repo.CrearTablas();
					break;
				case 3: // probar con access
					string archivoMDB="indices_canastaD3.mdb";
					Archivo.Borrar(archivoMDB);
					BdAccess.Crear(archivoMDB);
					db=BdAccess.Abrir(archivoMDB);
					repo=new RepositorioPruebaIndice(db);
					repo.CrearTablas();
					break;
			}
			#pragma warning restore 162
			Productos P100=new Productos(); P100.InsertarDirecto(db,"P100");
			Productos P101=repo.CrearProducto("P101");
			Productos P102=repo.CrearProducto("P102");
			Agrupaciones A=repo.CrearAgrupacion("A");
			Grupos A1=repo.CrearGrupo("A1",A,60);
			Grupos A2=repo.CrearGrupo("A2",A,40);
			Agrupaciones T=repo.CrearAgrupacion("T");
			Grupos TU=repo.AbrirGrupo("T","T");
			repo.CrearHoja(P100,A1,60);
			repo.CrearHoja(P101,A1,40);
			repo.CrearHoja(P102,A2,100);
			repo.CrearHoja(P100,TU,60);
			repo.CrearHoja(P101,TU,40);
			repo.CrearHoja(P102,TU,100);
			repo.CalcularPonderadores(A);
			repo.CalcularPonderadores(T);
		}
		[Test]
		public void A01CalculosBase(){
			Calculos pAnt=repo.CrearCalculo(2001,12,0);
			Productos P100=repo.AbrirProducto("P100");
			Productos P101=repo.AbrirProducto("P101");
			Productos P102=repo.AbrirProducto("P102");
			Agrupaciones A=repo.AbrirAgrupacion("A");
			repo.RegistrarPromedio(pAnt,P100,2.0);
			repo.RegistrarPromedio(pAnt,P101,10.0);
			repo.RegistrarPromedio(pAnt,P102,20.0);
			repo.CalcularMesBase(pAnt,A);
			Assert.AreEqual(100.0,new CalGru(repo.db,pAnt,A).cIndice.Valor);
			Calculos Per1=repo.CrearProximo(pAnt);
			Per1.UsarFk();
			Assert.AreEqual("200201",Per1.cPeriodo.Valor);
			Assert.AreEqual(2002,Per1.fkPeriodos.cAno.Valor);
			Assert.AreEqual(1,Per1.fkPeriodos.cMes.Valor);
			Grupos A1=repo.AbrirGrupo("A","A1");
			Grupos A2=repo.AbrirGrupo("A","A2");
			repo.RegistrarPromedio(Per1,P100,2.0);
			repo.RegistrarPromedio(Per1,P101,10.0);
			repo.RegistrarPromedio(Per1,P102,22.0);
			repo.CalcularCalGru(Per1,A);
			Assert.AreEqual(110.0,new CalGru(repo.db,Per1,A2).cIndice.Valor,Controlar.DeltaDouble);
			Assert.AreEqual(104.0,new CalGru(repo.db,Per1,A).cIndice.Valor,Controlar.DeltaDouble);
			Calculos Per2=repo.CrearProximo(Per1);
			repo.RegistrarPromedio(Per2,P100,2.2);
			repo.RegistrarPromedio(Per2,P101,11.0);
			repo.RegistrarPromedio(Per2,P102,22.0);
			repo.CalcularCalGru(Per2,A);
			Assert.AreEqual(110.0,new CalGru(repo.db,Per2,A2).cIndice.Valor,Controlar.DeltaDouble);
			Assert.AreEqual(110.0,new CalGru(repo.db,Per2,A).cIndice.Valor,Controlar.DeltaDouble);
			repo.ExpandirEspecificacionesYVariedades();
		}
		public void CargarPrecio(string periodo, string producto, int informante, double precio){
			RelVar r=new RelVar();
			r.InsertarValores(repo.db,r.cPeriodo.Es(periodo),r.cInformante.Es(informante),r.cVariedad.Es(producto),r.cPrecio.Es(precio));
		}
		[Test]
		public void A02CalculosTipoInf(){
			Informantes inf=new Informantes();
			inf.InsertarDirecto(repo.db,1);
			inf.InsertarDirecto(repo.db,2);
			inf.InsertarDirecto(repo.db,3);
			inf.InsertarDirecto(repo.db,4);
			Variedades v=new Variedades();
			v.InsertarValores(repo.db,v.cEspecificacion.Es("P100"),v.cVariedad.Es("P100/2"));
			CargarPrecio("200112","P100"	,1,2.0);
			CargarPrecio("200112","P100"	,2,2.0);
			CargarPrecio("200112","P100"	,4,3.0);
			CargarPrecio("200201","P100"	,1,2.0);
			CargarPrecio("200201","P100"	,4,3.0);
			CargarPrecio("200201","P100/2"	,1,3.60);
			CargarPrecio("200202","P100"	,1,2.0);
			CargarPrecio("200202","P100"	,2,2.2);
			CargarPrecio("200202","P100/2"	,1,2.60);
			CargarPrecio("200202","P100"	,3,2.4);
			CargarPrecio("200202","P100/2"	,3,2.60);
			CargarPrecio("200112","P101"	,2,12.2);
			CargarPrecio("200201","P101"	,2,12.2);
			Periodos p=new Periodos(); 
			Calculos c=new Calculos();
			c.cCalculo.AsignarValor(-1);
			c.cEsPeriodoBase.AsignarValor(true);
			c.InsertarValores(repo.db,c.cPeriodo.Es("200202"),c.cEsPeriodoBase,c);
			c.InsertarValores(repo.db,c.cPeriodo.Es("200201"),c.cEsPeriodoBase,c.cPeriodoAnterior.Es("200202"),c);
			c.InsertarValores(repo.db,c.cPeriodo.Es("200112"),c.cEsPeriodoBase,c.cPeriodoAnterior.Es("200201"),c);
			Assert.IsTrue(c.Buscar(repo.db,"200112",-1),"está el primer período");
			Assert.IsTrue(c.Buscar(repo.db,"200202",-1),"está el último período");
			repo.CalcularMatrizBase(2);
			object[,] esperado={
				{"200201","P100"	,4},
				{"200201","P101"	,2},
				{"200202","P100"	,1},
				{"200202","P100"	,2}
			};
			int cantidad=0;
			foreach(NovEspInf n in new NovEspInf().Todos(repo.db)){
				Assert.IsTrue(cantidad<esperado.GetLength(0),"Hay resultados de más");
				Assert.AreEqual(esperado[cantidad,2],n.cInformante.Valor);
				Assert.AreEqual(esperado[cantidad,0],n.cPeriodo.Valor);
				Assert.AreEqual(esperado[cantidad,1],n.cEspecificacion.Valor);
				Assert.AreEqual(-1,n.cCalculo.Valor);
				cantidad++;
			}
			Assert.AreEqual(esperado.GetLength(0),cantidad,"cantidad de registros vistos");
			object[,] esperado2={
				{"200112","P100"	,1,2.0},
				{"200112","P100"	,2,2.0},
				{"200112","P100"	,4,3.0},
				{"200112","P101"	,2,12.2},
				{"200201","P100"	,1,2.0},
				{"200201","P100"	,4,Math.Sqrt(3.0*3.60)},
				{"200201","P101"	,2,12.2},
				{"200202","P100"	,1,Math.Sqrt(2.0*2.60)},
				{"200202","P100"	,2,2.2}};
			cantidad=0;
			foreach(CalEspInf cei in new CalEspInf().Todos(repo.db)){
				Assert.IsTrue(cantidad<esperado2.GetLength(0),"Hay resultados de más");
				Assert.AreEqual(esperado2[cantidad,2],cei.cInformante.Valor);
				Assert.AreEqual(esperado2[cantidad,0],cei.cPeriodo.Valor);
				Assert.AreEqual(esperado2[cantidad,1],cei.cEspecificacion.Valor);
				Assert.AreEqual(-1,cei.cCalculo.Valor);
				Assert.AreEqual((double)esperado2[cantidad,3],(double)cei.cPromedio.Valor,0.001);
				cantidad++;
			}
			Assert.AreEqual(esperado2.GetLength(0),cantidad,"cantidad de registros vistos");
		}
		[Test]
		public void VerCanasta(){
			Grupos A=repo.AbrirGrupo("A","A");
			Grupos A1=repo.AbrirGrupo("A","A1");
			Productos P100=repo.AbrirProducto("P100");
			Assert.AreEqual(1.0,A.cPonderador.Valor,Controlar.DeltaDouble);
			Assert.AreEqual(0.6,A1.cPonderador.Valor,Controlar.DeltaDouble);
			Assert.AreEqual(0.36,P100.Ponderador(A),Controlar.DeltaDouble);
			Assert.AreEqual(0.6,P100.Ponderador(A1),Controlar.DeltaDouble);
		}
		[Test]
		public void zReglasDeIntegridad(){
			repo.ReglasDeIntegridad();
		}
	}
}
