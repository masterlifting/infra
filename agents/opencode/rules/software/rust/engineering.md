# Rust Engineering Rules

## Scope

- Canonical rule set for Rust code; prioritize correctness, memory safety, predictable performance, and readability.
- Prefer idiomatic Rust first; reach for `unsafe` only when safe Rust genuinely cannot express the requirement.
- Enforce style with `rustfmt`; lint with `clippy --all-targets --all-features -- -D warnings` in CI.
- Pin the edition and MSRV (Minimum Supported Rust Version) in `Cargo.toml`; document MSRV in `README` for library crates.
- Keep `cargo check`, `cargo test`, and `cargo clippy` warning-clean; treat warnings as design feedback.
- Use `cargo fmt --check` and `cargo clippy` as CI gates, not optional steps.

## Type Declarations

- Prefer `let` immutable bindings; use `let mut` only when mutation is required.
- Let inference do its work for local bindings; add explicit types when they aid readability or disambiguate inference.
- Use `const` for compile-time constants and `static` only when a single shared location is required.
- Use type aliases sparingly — they shorten type names but do not create new types; reach for the newtype pattern (`struct Name(T)`) when type identity matters.
- Use the newtype pattern for semantic primitives (`UserId(Uuid)`, `Email(String)`) to prevent primitive obsession.

## Ownership and Borrowing

- Default to taking parameters by reference (`&T`, `&str`, `&[T]`) over owned values; require ownership only when the function stores or transforms the value.
- Prefer `&str` over `&String` and `&[T]` over `&Vec<T>` in function signatures.
- Prefer borrowing over `.clone()`; reach for `.clone()` only when a borrow's lifetime constraints make the alternative materially worse.
- Make lifetimes explicit when elision misleads; otherwise let elision keep signatures small.
- Prefer `Cow<'_, T>` when a function sometimes needs to own and sometimes to borrow.
- Reach for `Rc`/`Arc` only when shared ownership is genuinely required; prefer single ownership with borrowing first.
- Use `Arc<T>` for cross-thread shared ownership; use `Rc<T>` for single-threaded shared ownership.
- Combine `Arc<Mutex<T>>` / `Arc<RwLock<T>>` only when shared mutation across threads is required; prefer channels for ownership transfer.

## Option and Nullability

- Use `Option<T>` for "may be absent" values; Rust has no `null` and you should not invent one.
- Convert nullable FFI / interop inputs to `Option<T>` at the boundary; never propagate raw pointer null-checks into domain code.
- Prefer combinators (`map`, `and_then`, `unwrap_or`, `unwrap_or_else`, `ok_or`) over `match` for simple `Option` transforms.
- Use `if let Some(x) = ...` / `let Some(x) = ... else { return ... }` (let-else) for early returns on absence.
- Never use `.unwrap()` or `.expect()` in library code or production paths unless the invariant is provably enforced; document the invariant with a comment when used. Reserve `// SAFETY:` for unsafe-code invariants only.

## Error Handling

- Return `Result<T, E>` for any operation that can fail in a recoverable way; use `panic!` only for invariant violations the program cannot continue past.
- Propagate errors with `?`; avoid manual `match` ladders for error forwarding.
- Use `thiserror` for library error enums (rich, typed variants); use `anyhow` for application/binary code where the caller only logs or aborts.
- Prefer concrete error enums at API boundaries; reserve `Box<dyn Error>` / `anyhow::Error` for application code.
- Implement `From<SourceError>` for crate error types so `?` composes cleanly.
- Avoid stringly-typed errors; carry enough structured context (codes, IDs) for the caller to act.
- Never use error types for normal control flow.
- Reserve `panic!`, `unreachable!`, `todo!` for genuine logic bugs; do not ship `todo!()` to production.

## Pattern Matching and Equality

- Prefer exhaustive `match`; resolve non-exhaustive warnings rather than catching with `_ => unreachable!()`.
- Use `if let` and `while let` for single-arm patterns.
- Use `let ... else` (let-else) for early-return destructuring.
- Use match guards (`Pattern if condition`) for refined arms; keep guards simple.
- Derive `PartialEq`/`Eq`/`Hash` when structural equality matches domain intent; implement manually only when semantics differ from structure.
- Prefer `matches!(x, Pattern)` for boolean checks against a pattern.

## Deconstruction and Discards

- Destructure tuples and structs in `let`, function params, and match arms to keep code flat.
- Use `_` for intentionally ignored values; do not name real bindings `_` (it does not bind).
- Use leading-underscore names (`_unused`) for intentionally unused bindings the compiler should not flag.
- Use `..` in struct patterns to ignore remaining fields explicitly.

## Functions and API Design

- Keep public signatures explicit and intention-revealing; document panics, errors, and safety requirements in rustdoc.
- Take iterators (`impl IntoIterator<Item = T>`) rather than concrete `Vec<T>` when the function only iterates.
- Take `impl AsRef<Path>` / `impl AsRef<str>` for conversion-friendly inputs.
- Return concrete types from public APIs by default; return `impl Trait` only when the concrete type is implementation detail.
- Prefer the builder pattern for structs with many optional fields; avoid telescoping constructors.
- Use `#[must_use]` on functions and types whose return value should not be silently dropped.

## Trait Design and Generics

- Name traits as verbs, nouns, or adjectives without grammatical suffixes — `Read`, `Write`, `Iterator`, `Display`, not `Readable`/`Writeable`.
- If a single method dominates a trait, give the trait the method's name (`Hash::hash`, `Clone::clone`).
- Prefer generics + trait bounds (static dispatch) for hot paths; prefer `dyn Trait` (dynamic dispatch) when heterogeneous collections or plugin-style APIs are required.
- Keep trait method sets small and focused; split unrelated capabilities into separate traits.
- Respect the orphan rule: implement foreign traits on local types or local traits on foreign types — never both foreign.
- Use sealed traits (`pub trait T: private::Sealed`) to prevent downstream impls when the contract is internal.
- Prefer blanket impls (`impl<T: Trait> OtherTrait for T`) for cross-cutting behavior; document constraints clearly.
- Derive `Debug` on virtually all public types; derive `Clone`/`Copy` only when semantically appropriate.

## Async and Concurrency

- Use `async fn` / `.await` for I/O-bound work; do not block the runtime with synchronous I/O.
- Pick one runtime per binary (typically Tokio) and stay consistent; document runtime requirements in library crates.
- Mark CPU-heavy work inside async with `tokio::task::spawn_blocking` (or the equivalent) so it does not stall the executor.
- Make futures `Send` when they will cross threads (default for multi-threaded runtimes); use `!Send` only intentionally.
- Use `tokio::select!` for racing futures; ensure cancel-safety of every branch.
- Use bounded channels (`mpsc::channel(n)`) for back-pressure; reserve unbounded channels for known-small fan-in.
- Prefer message passing over shared mutable state; reach for `Mutex`/`RwLock` only when ownership cannot be moved.
- Use `tokio::sync::Mutex` (async) when the lock crosses an `await`; use `std::sync::Mutex` for purely synchronous critical sections.
- Always thread `CancellationToken` (or equivalent) through long-running async work; never `.await` indefinitely without a cancellation path.

## Collections and Iterators

- Prefer iterator chains (`.iter().filter().map().collect()`) over manual loops where intent is clearer.
- Place filters early in the chain to reduce downstream work.
- Use `iter()` for borrows, `into_iter()` to consume, `iter_mut()` for mutable iteration.
- Use `collect::<Vec<_>>()` only when you need the materialized collection; otherwise stay lazy.
- Use `HashMap`/`HashSet` for O(1) lookup; use `BTreeMap`/`BTreeSet` when ordering or range queries matter.
- Pre-size collections (`Vec::with_capacity`, `HashMap::with_capacity_and_hasher`) when size is known.
- Avoid repeated `.clone()` inside iterator chains on hot paths.

## Memory and Allocation

- Profile before optimizing; do not assume allocation hotspots.
- Prefer stack allocation; reach for `Box<T>` only when heap is required (large values, recursive types, trait objects).
- Use `Cow<'_, T>` to defer allocation until mutation is needed.
- Use `Box<[T]>` instead of `Vec<T>` for fixed-size heap arrays that will not grow.
- Use `SmallVec` / `tinyvec` / `arrayvec` for short collections that mostly stay small.
- Choose `String` vs `&str` deliberately at API boundaries — `&str` for read-only views, `String` for owned/mutable text.
- Avoid `.to_string()` and `format!()` in tight loops; use `write!` into a reused buffer.

## Strings

- Use `&str` for borrowed slices and `String` for owned, growable strings; never use `String` where `&str` suffices.
- Use `write!` / `writeln!` macros into `String` or `Formatter` for building up text without intermediate allocations.
- Use `format!` for one-shot formatting; do not chain `format!` calls.
- Use `Display` for user-facing rendering and `Debug` for developer diagnostics; do not conflate them.
- Avoid embedding secrets or PII in `Debug` output; implement `Debug` manually to redact when needed.

## Unsafe Code

- Default to safe Rust; reach for `unsafe` only when the requirement cannot be expressed safely (FFI, raw pointer manipulation, performance-critical primitives).
- Keep `unsafe` blocks small, well-commented, and accompanied by a `// SAFETY:` comment documenting the invariants the caller upholds.
- Wrap `unsafe` behind safe abstractions; do not expose `unsafe fn` in public APIs unless invariants cannot be encapsulated.
- Run `cargo miri test` on crates that contain `unsafe` to catch undefined behavior.
- Audit `unsafe` blocks on every change; treat them as security-sensitive code.

## Logging and Diagnostics

- Use the `tracing` crate for structured logging and spans; use `log` only when integrating with libraries that target it.
- Emit structured fields, not pre-formatted strings: `tracing::info!(user_id = %id, "request handled")`.
- Use spans to attach correlation context across async boundaries.
- Never log secrets, tokens, or PII; redact at the source.
- Use `tracing_subscriber` with environment-driven filters (`RUST_LOG`) so verbosity is configurable without code changes.

## Module and Crate Organization

- Use `mod.rs` or named-file modules consistently across the crate.
- Keep modules small and focused; split when a file grows past comfortable navigation.
- Use `pub(crate)`, `pub(super)`, and `pub(in path)` to scope visibility narrowly; do not expose internals as `pub` to avoid private-type errors.
- Re-export the crate's public surface from `lib.rs` with `pub use` so consumers do not need to know internal layout.
- Group `use` imports: std, external crates, then crate-internal; let `rustfmt` enforce.
- Avoid `use foo::*` glob imports outside of preludes and tests.

## Cargo Manifest and Dependencies

- Pin or range dependencies thoughtfully; commit `Cargo.lock` for binaries, omit for libraries.
- Use Cargo features for optional capabilities; keep the default feature set minimal.
- Name features for what they enable (`serde`, `tokio`), not as `with-X` or `use-X`.
- Avoid pulling in heavy dependencies for trivial functionality; audit transitive weight with `cargo tree`.
- Run `cargo audit` / `cargo deny` in CI to catch advisories and license violations.
- Set `[profile.release]` opt-level, LTO, and codegen-units intentionally; do not rely on defaults for shipped binaries.

## Documentation

- Document every public item with rustdoc; show purpose, parameters, errors, panics, and at least one example.
- Run `cargo test --doc` to keep examples compiling.
- Use `#[doc(hidden)]` for items that must be public for macros but are not part of the supported API.
- Link related items with intra-doc links (`[Type]`, `[Type::method]`) rather than hand-written URLs.
